using DragonGameEngine.Core.Ecs;
using Silk.NET.Maths;

namespace DragonGameEngine.Core.Resources
{
    public class Texture
    {
        public uint Id { get; }
        public InnerTexture Data { get; private set; }
        public uint Generation { get; private set; }

        public Texture(uint id, InnerTexture data, uint generation)
        {
            Id = id;
            Data = data;
            Generation = generation;
        }

        public void UpdateTexture(InnerTexture data, uint generation)
        {
            Data = data;
            Generation = generation;
        }

        public void ResetGeneration()
        {
            Generation = EntityIdService.INVALID_ID;
        }
    }

    public readonly struct InnerTexture
    {
        public Vector2D<uint> Size { get; }
        public byte ChannelCount { get; }
        public bool HasTransparency { get; }
        public object InternalData { get; } //do something with this type later

        public InnerTexture(Vector2D<uint> size, byte channelCount, bool hasTransparency, object internalData)
        {
            Size = size;
            ChannelCount = channelCount;
            HasTransparency = hasTransparency;
            InternalData = internalData;
        }
    }
}
