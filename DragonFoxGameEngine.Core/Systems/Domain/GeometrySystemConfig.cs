using System;

namespace DragonGameEngine.Core.Systems.Domain
{
    /// <summary>
    /// Geometry System configuration settings.
    /// </summary>
    public readonly struct GeometrySystemConfig
    {
        /// <summary>
        /// Max number of geometries that can be laoded at once.
        /// </summary>
        /// <remarks>
        /// Should be significantly greater than the number of static meshes because
        /// there can and will be more than one of these per mesh.
        /// Take other systems into account as well.
        /// </remarks>
        public uint MaxGeometryCount { get; init; }

        public GeometrySystemConfig(uint maxGeometryCount)
        {
            if (maxGeometryCount == 0)
            {
                throw new ArgumentException("config.MaxGeometryCount must be > 0.");
            }
            MaxGeometryCount = maxGeometryCount;
        }
    }
}
