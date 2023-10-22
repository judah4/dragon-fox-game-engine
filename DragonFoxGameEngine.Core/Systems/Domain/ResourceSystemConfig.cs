using System;

namespace DragonGameEngine.Core.Systems.Domain
{
    /// <summary>
    /// Resource System configuration settings.
    /// </summary>
    public readonly struct ResourceSystemConfig
    {
        public uint MaxLoaderCount { get; init; }

        /// <summary>
        /// The relative base path for assets.
        /// </summary>
        public string AssetBasePath { get; init; }

        public ResourceSystemConfig(uint maxLoaderCount, string assetBasePath)
        {
            if (maxLoaderCount == 0)
            {
                throw new ArgumentException("config.MaxLoaderCount must be > 0.");
            }
            MaxLoaderCount = maxLoaderCount;
            AssetBasePath = assetBasePath;
        }
    }
}
