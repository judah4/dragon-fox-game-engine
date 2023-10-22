using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Resources.ResourceDataTypes;
using DragonGameEngine.Core.Systems.Domain;
using Foxis.Library;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;

namespace DragonGameEngine.Core.Systems
{
    public sealed class MaterialSystem
    {
        public const string DEFAULT_MATERIAL_NAME = "default";

        private readonly ILogger _logger;
        private readonly MaterialSystemConfig _config;
        private readonly TextureSystem _textureSystem;
        private readonly ResourceSystem _resourceSystem;
        private readonly Material _defaultMaterial;
        private readonly Dictionary<string, MaterialReference> _materials;

        private IRendererFrontend? _renderer;

        /// <summary>
        /// Number of materials loaded and referenced.
        /// </summary>
        public int MaterialsCount => _materials.Count;

        public MaterialSystem(ILogger logger, MaterialSystemConfig config, TextureSystem textureSystem, ResourceSystem resourceSystem)
        {
            _logger = logger;
            _config = config;
            _textureSystem = textureSystem;
            _materials = new Dictionary<string, MaterialReference>((int)config.MaxMaterialCount);
            _defaultMaterial = new Material(DEFAULT_MATERIAL_NAME);
            _resourceSystem = resourceSystem;
        }

        public void Init(IRendererFrontend renderer)
        {
            _renderer = renderer;
            CreateDefaultMaterial();

            _logger.LogInformation("Material System initialized");
        }

        public void Shutdown()
        {
            //destroy all loaded materials.
            foreach (var refPair in _materials)
            {
                if (refPair.Value.Handle.Generation == EntityIdService.INVALID_ID)
                {
                    continue;
                }
                DestroyMaterial(refPair.Value.Handle);
            }
            _materials.Clear();

            DestroyMaterial(_defaultMaterial);

            _logger.LogInformation("Material System shutdown");
        }

        public Material GetDefaultMaterial()
        {
            return _defaultMaterial;
        }

        public Material Acquire(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Equals(DEFAULT_MATERIAL_NAME))
            {
                return _defaultMaterial;
            }

            var resource = _resourceSystem.Load(name, ResourceType.Material);
            var config = (MaterialConfig)resource.Data;

            //Now Acquire from loaded config
            var material = AcquireFromConfig(config);

            _resourceSystem.Unload(resource);
            return material;
        }

        public Material AcquireFromConfig(MaterialConfig materialConfig)
        {
            if (materialConfig.Name.Equals(DEFAULT_MATERIAL_NAME))
            {
                return _defaultMaterial;
            }

            //Find the existing reference
            if (!_materials.TryGetValue(materialConfig.Name, out var materialRef))
            {
                materialRef = new MaterialReference()
                {
                    ReferenceCount = 0,
                    AutoRelease = materialConfig.AutoRelease, //set only the first time it's loaded
                    Handle = new Material(materialConfig.Name),
                };
                _materials.Add(materialConfig.Name, materialRef);
            }
            materialRef.ReferenceCount++;

            //load if it hasn't been loaded yet
            if (materialRef.Handle.Generation == EntityIdService.INVALID_ID)
            {
                var loadResult = LoadMaterial(materialConfig, materialRef.Handle);
                if (loadResult.IsFailure)
                {
                    var errorMessage = $"Load Material Error - {loadResult.Error}";
                    _logger.LogError(errorMessage);
                    throw new EngineException(errorMessage);
                }
                _logger.LogDebug("Material {name} does not exist yet. Created and ref count is now {refCount}", materialConfig.Name, materialRef.ReferenceCount);
            }

            _materials[materialConfig.Name] = materialRef;

            return materialRef.Handle;
        }

        public void Release(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Equals(DEFAULT_MATERIAL_NAME))
            {
                return;
            }
            //Find the existing material reference
            if (!_materials.TryGetValue(name, out var materialRef))
            {
                _logger.LogWarning("Tried to release a non-existing material {name}", name);
                return;
            }
            if (materialRef.ReferenceCount > 0)
            {
                materialRef.ReferenceCount--;
            }
            else
            {
                _logger.LogWarning("Material {name} was already at 0 references. Cleaning up", name);
            }

            _materials[name] = materialRef;

            if (materialRef.ReferenceCount == 0 && materialRef.AutoRelease)
            {
                //release material
                DestroyMaterial(materialRef.Handle);
                _materials.Remove(name);

                //TODO: pool thse material classes later so they don't hit the GC.
            }

            _logger.LogDebug("Release material {name}, now has a ref count of {refCount} (auto release = {autoRelease})", name, materialRef.ReferenceCount, materialRef.AutoRelease);
        }

        private Result<bool> LoadMaterial(MaterialConfig materialConfig, Material material)
        {
            if (_renderer == null)
            {
                throw new EngineException("Renderer not initialized!");
            }
            Texture diffuseTexture;
            try
            {
                diffuseTexture = _textureSystem.Acquire(materialConfig.DiffuseMapName, true);
            }
            catch (EngineException e)
            {
                _logger.LogError(e, "Unable to load texture {textName} for material {matName}, using default. {eMessage}", e.Message, materialConfig.DiffuseMapName, material.Name);
                diffuseTexture = _textureSystem.GetDefaultTexture();
            }

            var currentGeneration = material.Generation;

            try
            {
                TextureMap diffuseMap = new TextureMap() 
                {
                    TextureUse = TextureUse.Unknown,
                };
                if(!string.IsNullOrEmpty(materialConfig.DiffuseMapName))
                {
                    diffuseMap = new TextureMap()
                    {
                        TextureUse = TextureUse.MapDiffuse,
                        Texture = diffuseTexture,
                    };
                }

                //TODO: other maps

                material.UpdateMetaData(materialConfig.DiffuseColor, diffuseMap);

                _renderer.LoadMaterial(material);

                uint newGeneration = 0;
                if (currentGeneration != EntityIdService.INVALID_ID)
                {
                    newGeneration = currentGeneration + 1;
                }

                material.UpdateGeneration(newGeneration);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Result.Fail<bool>(e.Message);
            }
            return Result.Ok<bool>();
        }

        private void DestroyMaterial(Material material)
        {
            _logger.LogDebug("Destroying material {name}", material.Name);

            //Release texture references
            if(material.DiffuseMap.Texture != null && material.DiffuseMap.Texture.Name != TextureSystem.DEFAULT_TEXTURE_NAME)
            {
                _textureSystem.Release(material.DiffuseMap.Texture.Name);
            }

            //release renderer resources
            _renderer?.DestroyMaterial(material);

            //invalidate
            material.UpdateMetaData(material.DiffuseColor, new TextureMap() { TextureUse = TextureUse.Unknown });
            material.ResetGeneration();
        }

        private void CreateDefaultMaterial()
        {
            //white, default texture
            _defaultMaterial.UpdateMetaData(Vector4D<float>.One, new TextureMap()
            {
                TextureUse = TextureUse.MapDiffuse,
                Texture = _textureSystem.GetDefaultTexture(),
            });

            _renderer?.LoadMaterial(_defaultMaterial);
        }
    }
}
