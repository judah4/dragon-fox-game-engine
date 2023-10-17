using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DragonGameEngine.Core.Systems
{
    public sealed class MaterialSystem
    {
        public const string DEFAULT_MATERIAL_NAME = "default";


        private readonly ILogger _logger;
        private readonly MaterialSystemConfig _config;
        public Material DefaultMaterial { get; init; }
        public Dictionary<string, MaterialReference> Materials { get; init; }

        private IRenderer? _renderer;

        public MaterialSystem(ILogger logger, MaterialSystemConfig config)
        {
            _logger = logger;
            _config = config;
            Materials = new Dictionary<string, MaterialReference>((int)config.MaxMaterialCount);
            DefaultMaterial = new Material(DEFAULT_MATERIAL_NAME);
        }

        public void Init(IRenderer renderer)
        {
            _renderer = renderer;
            CreateDefaultMaterial();
        }

        public void Shutdown()
        {
            //destroy all loaded materials.
            //destroy all loaded textures.
            foreach (var refPair in Materials)
            {
                if (refPair.Value.Handle.Generation == EntityIdService.INVALID_ID)
                {
                    continue;
                }
                DestroyMaterial(refPair.Value.Handle);
            }
            Materials.Clear();

            DestroyMaterial(DefaultMaterial);
        }

        public Material Acquire(string name)
        {
            throw new NotImplementedException();
        }

        public Material AcquireFromConfig(string name, MaterialConfig materialConfig)
        {
            throw new NotImplementedException();
        }

        public Material Release(string name)
        {
            throw new NotImplementedException();
        }

        private void CreateDefaultMaterial()
        {
            throw new NotImplementedException();
        }

        private void LoadMaterial(MaterialConfig materialConfig, Material material)
        {
            throw new NotImplementedException();
        }

        private void DestroyMaterial(Material material)
        {
            throw new NotImplementedException();
        }

        private MaterialConfig LoadConfigurationFile(string path)
        {
            throw new NotImplementedException();
        }

    }
}
