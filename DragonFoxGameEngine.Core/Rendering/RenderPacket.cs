using System.Collections.Generic;

namespace DragonGameEngine.Core.Rendering
{
    public readonly struct RenderPacket
    {
        public double DeltaTime { get; }

        /// <summary>
        /// The geoemtries
        /// </summary>
        /// <remarks>
        /// I should probably use Immutable list or FasterReadOnlyList for this.
        /// </remarks>
        public IReadOnlyList<GeometryRenderData> Geometries { get; }

        public RenderPacket(double deltaTime, IReadOnlyList<GeometryRenderData> geometries)
        {
            DeltaTime = deltaTime;
            Geometries = geometries;
        }
    }
}
