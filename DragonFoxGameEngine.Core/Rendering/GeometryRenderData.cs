using DragonGameEngine.Core.Resources;
using Silk.NET.Maths;

namespace DragonGameEngine.Core.Rendering
{
    /// <summary>
    /// Model info for rendering
    /// </summary>
    public struct GeometryRenderData
    {
        public uint ObjectId;
        public Matrix4X4<float> Model;
        public Texture[] Textures;
    }
}
