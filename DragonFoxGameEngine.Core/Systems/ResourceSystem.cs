using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems.Domain;
using DragonGameEngine.Core.Systems.ResourceLoaders;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace DragonGameEngine.Core.Systems
{
    public sealed class ResourceSystem
    {
        private readonly ILogger _logger;
        private readonly ResourceSystemConfig _config;
        private readonly Dictionary<ResourceType, IResourceLoader> _registeredLoaders;


        public ResourceSystem(ILogger logger, ResourceSystemConfig config)
        {
            _logger = logger;
            _config = config;
            _registeredLoaders = new Dictionary<ResourceType, IResourceLoader>((int)config.MaxLoaderCount);
        }

        public void Init()
        {
            //Auto-register known loader types
            RegisterLoader(new ImageResourceLoader(_logger, ResourceBasePath()));
            RegisterLoader(new TextResourceLoader(_logger, ResourceBasePath()));


            _logger.LogInformation("Resource System initialized with base path {path}", _config.AssetBasePath);
        }

        public void Shutdown()
        {
            _registeredLoaders.Clear();

            _logger.LogInformation("Resource System shutdown");
        }

        public void RegisterLoader(IResourceLoader loader)
        {
            if(_registeredLoaders.ContainsKey(loader.ResourceType))
            {
                throw new EngineException($"Resource Loader of type {loader.ResourceType} already exists and will not be registered.");
            }
            _registeredLoaders.Add(loader.ResourceType, loader);
            _logger.LogTrace("Loader {ltype} registered.", loader.ResourceType);
        }

        public Resource Load(string name, ResourceType type)
        {
            if (!_registeredLoaders.TryGetValue(type, out var loader))
            {
                throw new EngineException($"Resource Loader of type {type} does not exist");
            }

            return loader.Load(name);
        }

        public void Unload(Resource resource)
        {
            if (!_registeredLoaders.TryGetValue(resource.ResourceType, out var loader))
            {
                throw new EngineException($"Resource Loader of type {resource.ResourceType} does not exist so resource {resource.Name} is invalid.");
            }
            loader.Unload(resource);
            //invalidate resource
        }

        public string ResourceBasePath()
        {
            return _config.AssetBasePath;
        }
    }
}
