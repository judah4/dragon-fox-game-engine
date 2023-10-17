namespace DragonGameEngine.Core.Systems.Domain
{
    public readonly struct MaterialSystemConfig
    {
        public uint MaxMaterialCount { get; init; }

        public MaterialSystemConfig(uint maxMaterialCount)
        {
            MaxMaterialCount = maxMaterialCount;
        }
    }
}
