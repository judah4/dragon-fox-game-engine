using DragonGameEngine.Core.Resources;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            throw new NotImplementedException();
        }

        public void Unload(Resource resource)
        {
            throw new NotImplementedException();
        }
    }
}
