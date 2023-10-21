using DragonGameEngine.Core.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonGameEngine.Core.Systems.ResourceLoaders
{
    public interface IResourceLoader
    {
        ResourceType ResourceType { get; }
        string TypePath { get; }

        Resource Load(string name);
        void Unload(Resource resource);
    }
}
