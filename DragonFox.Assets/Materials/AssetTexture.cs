using DragonFox.Abstractions;

namespace DragonFox.Assets.Materials;

/// <summary>
/// In-Memory texture data
/// </summary>
public sealed class AssetTexture
{
    public byte ChannelCount { get; init; }

    public Vector2 Size { get; init; }

    public IReadOnlyCollection<byte> Pixels { get; init; }

    public bool HasTransparency { get; init; }

    public AssetTexture(byte channelCount, Vector2 size, IReadOnlyCollection<byte> pixels, bool hasTransparency)
    {
        ChannelCount = channelCount;
        Size = size;
        Pixels = [.. pixels];
        HasTransparency = hasTransparency;
    }
}