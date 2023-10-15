﻿using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems.Domain;
using Foxis.Library;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;

namespace DragonGameEngine.Core.Systems
{
    public sealed class TextureSystem
    {
        public const string DEFAULT_TEXTURE_NAME = "default";

        private readonly ILogger _logger;

        private TextureSystemState _state;
        private IRenderer _renderer;

        public TextureSystem(ILogger logger, IRenderer renderer, TextureSystemState state)
        {
            _logger = logger;
            _renderer = renderer;
            _state = state;
        }

        public void Initialize()
        {
            var textures = CreateDefaultTextures();
            _state.DefaultTexture.UpdateTexture(textures, EntityIdService.INVALID_ID);
        }

        public void Shutdown()
        {
            if (_state == null)
            {
                return;
            }
            //destroy all loaded textures.
            foreach (var textureRefPair in _state.Textures) 
            {
                if(textureRefPair.Value.TextureHandle.Generation == EntityIdService.INVALID_ID)
                {
                    continue;
                }
                _renderer.DestroyTexture(textureRefPair.Value.TextureHandle);
            }
            _state.Textures.Clear();

            DestroyDefaultTextures();
        }

        public Texture Acquire(string name, bool autoRelease)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (_state == null)
            {
                throw new EngineException("Texture System is not initialized.");
            }
            if (name.Equals(DEFAULT_TEXTURE_NAME))
            {
                _logger.LogWarning("TexureSystem Aquire called for default texture. Use GetDefaultTexture for texture 'default'");
                return _state.DefaultTexture;
            }

            //Find the existing texture reference
            if(!_state.Textures.TryGetValue(name, out var textureRef))
            {
                textureRef = new TextureReference()
                {
                    ReferenceCount = 0,
                    AutoRelease = autoRelease, //set only the first time it's loaded
                    TextureHandle = new Texture(unchecked((uint)name.GetHashCode()), default, EntityIdService.INVALID_ID),
                };
                _state.Textures.Add(name, textureRef);
            }
            textureRef.ReferenceCount++;

            //load if it hasn't been loaded yet
            if(textureRef.TextureHandle.Generation == EntityIdService.INVALID_ID)
            {
                var loadResult = LoadTexture(name, textureRef.TextureHandle);
                if(loadResult.IsFailure)
                {
                    var errorMessage = $"Load Texture Error - {loadResult.Error}";
                    _logger.LogError(errorMessage);
                    throw new EngineException(errorMessage);
                }
                _logger.LogDebug("Texture {textName} does not exist yet. Created and ref count is now {refCount}", name, textureRef.ReferenceCount);
            }

            _state.Textures[name] = textureRef;

            return textureRef.TextureHandle;
        }

        public void Release(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (_state == null)
            {
                throw new EngineException("Texture System is not initialized.");
            }
            if (name.Equals(DEFAULT_TEXTURE_NAME))
            {
                _logger.LogWarning("TexureSystem Tried to release the default texture. Don't do that.");
                return;
            }
            //Find the existing texture reference
            if (!_state.Textures.TryGetValue(name, out var textureRef))
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

            _state.Textures[name] = textureRef;

            if (textureRef.ReferenceCount == 0 && textureRef.AutoRelease)
            {
                //release texture
                _renderer.DestroyTexture(textureRef.TextureHandle);

                textureRef.TextureHandle.ResetGeneration();
                _state.Textures.Remove(name);

                //TODO: pool thse texture classes later so they don't hit the GC.
            }

            _logger.LogDebug("Release texture {name}, now has a ref count of {refCount} (auto release = {autoRelease})", name, textureRef.ReferenceCount, textureRef.AutoRelease);
        }

        public Texture GetDefaultTexture()
        {
            if (_state == null)
            {
                throw new EngineException("Texture System is not initialized.");
            }
            return _state.DefaultTexture;
        }

        private InnerTexture CreateDefaultTextures()
        {
            // NOTE: Create default texture, a 256x256 blue/white checkerboard pattern.
            // This is done in code to eliminate asset dependencies.
            _logger.LogDebug("Creating default texture...");
            const uint tex_dimension = 256;
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

            var innerTexture = _renderer.CreateTexture(
                "default",
                new Vector2D<uint>(tex_dimension, tex_dimension),
                4,
                pixels,
                false);

            return innerTexture;
        }

        private void DestroyDefaultTextures()
        {
            if(_state == null)
            {
                return;
            }
            _renderer.DestroyTexture(_state.DefaultTexture);
        }

        private Result<bool> LoadTexture(string textureName, Texture texture)
        {
            //TODO: should be able to be located anywhere.
            var path = "Assets/Textures/";
            const int requiredChannelCount = 4;

            //todo: try different extensions
            var filePath = Path.Join(path, $"{textureName}.png");

            try
            {
                using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(filePath);

                ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);
                var pixels = new byte[imageSize];
                img.CopyPixelDataTo(pixels);

                uint currentGeneration = texture.Generation;
                texture.ResetGeneration();

                // Check for transparency
                bool hasTransparency = img.PixelType.AlphaRepresentation.HasValue && img.PixelType.AlphaRepresentation.Value != PixelAlphaRepresentation.None;

                var innerTexture = _renderer.CreateTexture(
                    textureName,
                    new Vector2D<uint>((uint)img.Width, (uint)img.Height),
                    requiredChannelCount,
                    pixels,
                    hasTransparency);

                _renderer.DestroyTexture(texture);

                uint newGeneration = 0;
                if (currentGeneration != EntityIdService.INVALID_ID)
                {
                    newGeneration = currentGeneration + 1;
                }

                texture.UpdateTexture(innerTexture, newGeneration);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Result.Fail<bool>(e.Message);
            }
            return Result.Ok<bool>();
        }

    }
}
