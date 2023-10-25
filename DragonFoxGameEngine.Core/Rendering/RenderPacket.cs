using System.Collections.Generic;

namespace DragonGameEngine.Core.Rendering
{
    public readonly struct RenderPacket
    {
        public double DeltaTime { get; }

        /// <summary>
        /// The geomtries
        /// </summary>
        /// <remarks>
        /// I should probably use Immutable list or FasterReadOnlyList for this.
        /// </remarks>
        public IReadOnlyList<GeometryRenderData> Geometries { get; }

        /// <summary>
        /// The UI geomtries
        /// </summary>
        /// <remarks>
        /// I should probably use Immutable list or FasterReadOnlyList for this.
        /// </remarks>
        public IReadOnlyList<GeometryRenderData> UiGeometries { get; }

        public RenderPacket(double deltaTime, IReadOnlyList<GeometryRenderData> geometries, IReadOnlyList<GeometryRenderData> uiGeometries)
        {
            DeltaTime = deltaTime;
            Geometries = geometries;
            UiGeometries = uiGeometries;
        }
    }
}
