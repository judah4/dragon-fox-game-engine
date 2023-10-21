using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Resources;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace DragonGameEngine.Core.Systems.ResourceLoaders
{
    public sealed class BinaryResourceLoader : IResourceLoader
    {
        public ResourceType ResourceType => ResourceType.Binary;

        public string TypePath => "";


        private readonly ILogger _logger;
        private readonly string _basePath;

        public BinaryResourceLoader(ILogger logger, string basePath)
        {
            _logger = logger;
            _basePath = basePath;
        }

        public Resource Load(string name)
        {
            return BinData(name);
        }

        public void Unload(Resource resource)
        {
            resource.SetData(0, Array.Empty<byte[]>());
        }

        private Resource BinData(string name)
        {
            //todo: try different extensions
            var filePath = Path.Combine(_basePath, TypePath, name);

            var bytes = File.ReadAllBytes(filePath);

            if(bytes == null || bytes.Length < 1)
            {
                throw new ResourceException(name, $"Binary resource is empty!");
            }

            return new Resource(ResourceType, name, filePath, (ulong)bytes.Length, bytes);
        }
    }
}
