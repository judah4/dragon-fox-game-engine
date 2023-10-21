using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Resources.ResourceDataTypes;
using DragonGameEngine.Core.Systems.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;

namespace DragonGameEngine.Core.Systems
{
    public sealed class TextureSystem
    {
        public const string DEFAULT_TEXTURE_NAME = "default";

        private readonly ILogger _logger;

        public readonly TextureSystemConfig _config;
        public readonly Texture _defaultTexture;
        public readonly Dictionary<string, TextureReference> _textures;
        public readonly ResourceSystem _resourceSystem;

        private IRendererFrontend? _renderer;

        /// <summary>
        /// Number of textures loaded and referenced.
        /// </summary>
        public int TexturesCount => _textures.Count;

        public TextureSystem(ILogger logger, TextureSystemConfig config, ResourceSystem resources)
        {
            _logger = logger;
            _defaultTexture = new Texture(DEFAULT_TEXTURE_NAME);
            _textures = new Dictionary<string, TextureReference>((int)config.MaxTextureCount);
            _resourceSystem = resources;
        }

        public void Init(IRendererFrontend renderer)
        {
            _renderer = renderer;
            CreateDefaultTextures();

            _logger.LogInformation("Texture System initialized");

        }

        public void Shutdown()
        {
            //destroy all loaded textures.
            foreach (var textureRefPair in _textures) 
            {
                if(textureRefPair.Value.TextureHandle.Generation == EntityIdService.INVALID_ID)
                {
                    continue;
                }
                DestroyTexture(textureRefPair.Value.TextureHandle);
            }
            _textures.Clear();

            DestroyDefaultTextures();

            _logger.LogInformation("Texture System shutdown");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="autoRelease"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="EngineException">Throws if there is a loading issue.</exception>
        public Texture Acquire(string name, bool autoRelease)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Equals(DEFAULT_TEXTURE_NAME))
            {
                _logger.LogWarning("TexureSystem Aquire called for default texture. Use GetDefaultTexture for texture 'default'");
                return _defaultTexture;
            }

            //Find the existing texture reference
            if(!_textures.TryGetValue(name, out var textureRef))
            {
                textureRef = new TextureReference()
                {
                    ReferenceCount = 0,
                    AutoRelease = autoRelease, //set only the first time it's loaded
                    TextureHandle = new Texture(name),
                };
                _textures.Add(name, textureRef);
            }
            textureRef.ReferenceCount++;

            //load if it hasn't been loaded yet
            if(textureRef.TextureHandle.Generation == EntityIdService.INVALID_ID)
            {
                LoadTexture(textureRef.TextureHandle);
                _logger.LogDebug("Texture {textName} does not exist yet. Created and ref count is now {refCount}", name, textureRef.ReferenceCount);
            }

            _textures[name] = textureRef;

            return textureRef.TextureHandle;
        }

        public void Release(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Equals(DEFAULT_TEXTURE_NAME))
            {
                _logger.LogWarning("TexureSystem tried to release the default texture. Don't do that.");
                return;
            }
            //Find the existing texture reference
            if (!_textures.TryGetValue(name, out var textureRef))
            {
                _logger.LogWarning("Tried to release a non-existing texture {name}", name);
                return;
            }
            if(textureRef.ReferenceCount > 0)
            {
                textureRef.ReferenceCount--;
            }
            else
            {
                _logger.LogWarning("Texture {name} was already at 0 references. Cleaning up", name);
            }

            _textures[name] = textureRef;

            if (textureRef.ReferenceCount == 0 && textureRef.AutoRelease)
            {
                //release texture
                DestroyTexture(textureRef.TextureHandle);
                _textures.Remove(name);

                //TODO: pool thse texture classes later so they don't hit the GC.
            }

            _logger.LogDebug("Release texture {name}, now has a ref count of {refCount} (auto release = {autoRelease})", name, textureRef.ReferenceCount, textureRef.AutoRelease);
        }

        public Texture GetDefaultTexture()
        {
            return _defaultTexture;
        }

        private Texture CreateDefaultTextures()
        {
            if(_renderer == null)
            {
                throw new EngineException("Renderer not initialized!");
            }
            // NOTE: Create default texture, a 256x256 blue/white checkerboard pattern.
            // This is done in code to eliminate asset dependencies.
            _logger.LogDebug("Creating default texture...");
            const uint tex_dimension = 128;
            const uint channels = 4;
            const uint pixel_count = tex_dimension * tex_dimension;
            byte[] pixels = new byte[pixel_count * channels];
            Array.Fill<byte>(pixels, 255); //set full
            // Each pixel.
            for (uint row = 0; row < tex_dimension; ++row)
            {
                for (uint col = 0; col < tex_dimension; ++col)
                {
                    //this is all hard to follow but it makes a tile.
                    uint index = (row * tex_dimension) + col;
                    uint index_bpp = index * channels;
                    if (row % 2 == 0)
                    {
                        if (col % 2 == 0)
                        {
                            pixels[index_bpp + 0] = 0;
                            pixels[index_bpp + 1] = 0;
                        }
                    }
                    else
                    {
                        if (!(col % 2 == 0))
                        {
                            pixels[index_bpp + 0] = 0;
                            pixels[index_bpp + 1] = 0;
                        }
                    }
                }
            }

            _defaultTexture.UpdateTextureMetaData(
                new Vector2D<uint>(tex_dimension, tex_dimension),
                4,
                false);

            _renderer.LoadTexture(pixels, _defaultTexture);
            return _defaultTexture;
        }

        private void DestroyDefaultTextures()
        {
            DestroyTexture(_defaultTexture);
        }

        /// <summary>
        /// Loads texture from disk, eventually from resources.
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        /// <exception cref="EngineException">Thrown if renderer is not initialized</exception>
        /// <remarks>
        /// Throws a bunch of errors so catch if you can.
        /// </remarks>
        private void LoadTexture(Texture texture)
        {
            if(_renderer == null)
            {
                throw new EngineException("texture system not initialized with renderer!");
            }

            var resource = _resourceSystem.Load(texture.Name, ResourceType.Image);
            var imageResourceData = (ImageResourceData)resource.Data;

            uint currentGeneration = texture.Generation;
            texture.ResetGeneration();

            _renderer.DestroyTexture(texture); //destroy/release the old

            texture.UpdateTextureMetaData(
                imageResourceData.Size,
                imageResourceData.ChannelCount,
                imageResourceData.HasTransparency);

            _renderer.LoadTexture(imageResourceData.Pixels, texture);

            uint newGeneration = 0;
            if (currentGeneration != EntityIdService.INVALID_ID)
            {
                newGeneration = currentGeneration + 1;
            }

            texture.UpdateGeneration(newGeneration);
            
            //clean up just in case
            _resourceSystem.Unload(resource);
        }

        private void DestroyTexture(Texture texture) 
        {
            _renderer?.DestroyTexture(texture);
            texture.ResetGeneration();
        }

    }
}
