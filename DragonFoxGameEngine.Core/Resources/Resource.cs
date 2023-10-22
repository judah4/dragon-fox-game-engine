using System;

namespace DragonGameEngine.Core.Resources
{
    public sealed class Resource
    {
        public ResourceType ResourceType { get; init; }
        public string Name { get; init; }
        public string FullPath { get; init; }
        public ulong DataSize { get; private set; }
        public object Data { get; private set; }

        public Resource(ResourceType resourceType, string name, string fullPath, ulong dataSize, object data)
        {
            ResourceType = resourceType;
            Name = name;
            FullPath = fullPath;
            DataSize = dataSize;
            Data = data;
        }

        public void Unload()
        {
            DataSize = 0;
        }

        public void SetData(ulong dataSize, object data)
        {
            DataSize = dataSize;
            Data = data;
        }
    }
}
