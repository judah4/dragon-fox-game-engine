using DragonGameEngine.Core.Resources;
using Silk.NET.Maths;

namespace DragonGameEngine.Core.Rendering
{
    /// <summary>
    /// Model info for rendering
    /// </summary>
    public readonly struct GeometryRenderData
    {
        public Matrix4X4<float> Model { get; init; }
        public  Geometry Geometry { get; init; }
    }
}
