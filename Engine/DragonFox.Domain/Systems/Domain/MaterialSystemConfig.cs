using System;

namespace DragonGameEngine.Core.Systems.Domain
{
    public readonly struct MaterialSystemConfig
    {
        public uint MaxMaterialCount { get; init; }

        public MaterialSystemConfig(uint maxMaterialCount)
        {
            if (maxMaterialCount == 0)
            {
                throw new ArgumentException("config.MaxMaterialCount must be > 0.");
            }
            MaxMaterialCount = maxMaterialCount;
        }
    }
}
