namespace DragonGameEngine.Core.Systems.Domain
{
    public readonly struct TextureSystemConfig
    {
        public uint MaxTextureCount { get; init; }

        public TextureSystemConfig(uint maxTextureCount)
        {
            MaxTextureCount = maxTextureCount;
        }
    }
}
