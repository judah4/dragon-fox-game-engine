using DragonGameEngine.Core.Ecs;
using Silk.NET.Maths;
using System;

namespace DragonGameEngine.Core.Resources
{
    public sealed class Texture
    {
        public const int NAME_MAX_LENGTH = 512;

        public uint Id { get; }

        public string Name { get; }

        public Vector2D<uint> Size { get; private set; }
        public byte ChannelCount { get; private set; }
        public bool HasTransparency { get; private set; }
        public object InternalData { get; private set; } //do something with this type later

        public uint Generation { get; private set; }

        public Texture(string name) : this(name, Vector2D<uint>.Zero, 0, false, false, EntityIdService.INVALID_ID)
        {
        }

        public Texture(string name, Vector2D<uint> size, byte channelCount, bool hasTransparency, object internalData, uint generation)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), "Name should not be null or empty.");
            }
            if(name.Length > NAME_MAX_LENGTH)
            {
                throw new ArgumentException(nameof(name), $"Name should not be less than {NAME_MAX_LENGTH}");
            }

            Id = unchecked((uint)name.GetHashCode());
            Name = name;
            Size = size;
            ChannelCount = channelCount;
            HasTransparency = hasTransparency;
            InternalData = internalData;
            Generation = generation;
        }

        public void UpdateTextureMetaData(Vector2D<uint> size, byte channelCount, bool hasTransparency)
        {
            Size = size;
            ChannelCount = channelCount;
            HasTransparency = hasTransparency;
        }

        public void UpdateTextureInternalData(object internalData)
        {
            InternalData = internalData;
        }

        public void ResetGeneration()
        {
            Generation = EntityIdService.INVALID_ID;
        }

        public void UpdateGeneration(uint generation)
        {
            Generation = generation;
        }
    }
}
