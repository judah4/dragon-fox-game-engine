using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Resources;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DragonGameEngine.Core.Systems.ResourceLoaders
{
    public sealed class TextResourceLoader : IResourceLoader
    {
        public ResourceType ResourceType => ResourceType.Text;

        public string TypePath => "Texts";


        private readonly ILogger _logger;
        private readonly string _basePath;

        public TextResourceLoader(ILogger logger, string basePath)
        {
            _logger = logger;
            _basePath = basePath;
        }

        public Resource Load(string name)
        {
            return TextLoad(name);
        }

        public void Unload(Resource resource)
        {
            resource.Unload();
        }

        private Resource TextLoad(string name)
        {
            //todo: try different extensions
            var filePath = Path.Combine(_basePath, TypePath, $"{name}.txt");

            var text = File.ReadAllText(filePath, Encoding.UTF8);

            if(string.IsNullOrEmpty(text))
            {
                throw new ResourceException(name, $"Text resource is empty!");
            }

            return new Resource(ResourceType, name, filePath, (ulong)text.Length, text);
        }
    }
}
