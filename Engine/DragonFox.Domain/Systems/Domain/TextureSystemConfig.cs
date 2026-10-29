using System;

namespace DragonGameEngine.Core.Systems.Domain
{
    public readonly struct TextureSystemConfig
    {
        public uint MaxTextureCount { get; init; }

        public TextureSystemConfig(uint maxTextureCount)
        {
            if (maxTextureCount == 0)
            {
                throw new ArgumentException("config.MaxTextureCount must be > 0.");
            }
            MaxTextureCount = maxTextureCount;
        }
    }
}
