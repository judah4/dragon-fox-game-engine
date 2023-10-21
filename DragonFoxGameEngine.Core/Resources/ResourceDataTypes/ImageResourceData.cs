using Silk.NET.Maths;

namespace DragonGameEngine.Core.Resources.ResourceDataTypes
{
    /// <remarks>
    /// I might change these to assets later
    /// </remarks>
    public readonly struct ImageResourceData
    {
        public byte ChannelCount { get; init; }

        public Vector2D<uint> Size { get; init; }

        public byte[] Pixels { get; init; }

        public ImageResourceData(byte channelCount, Vector2D<uint> size, byte[] pixels)
        {
            ChannelCount = channelCount;
            Size = size;
            Pixels = pixels;
        }

    }
}
