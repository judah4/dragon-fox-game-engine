namespace DragonGameEngine.Core.Systems.Domain
{
    public record TextureSystemConfig
    {
        public readonly uint MaxTextureCount;

        public TextureSystemConfig(uint maxTextureCount)
        {
            MaxTextureCount = maxTextureCount;
        }
    }
}
