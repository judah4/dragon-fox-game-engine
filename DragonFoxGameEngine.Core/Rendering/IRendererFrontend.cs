using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Resources;
using Silk.NET.Maths;
using System;

namespace DragonGameEngine.Core.Rendering
{
    public interface IRendererFrontend
    {
        /// <summary>
        /// Init renderer
        /// </summary>
        public void Init();
        public void Shutdown();

        public void Resized(Vector2D<uint> size);

        public void DrawFrame(RenderPacket packet);

        public void SetView(Matrix4X4<float> view);

        public void LoadTexture(Span<byte> pixels, Texture texture);
        public void DestroyTexture(Texture texture);

        public void LoadMaterial(Material material);
        public void DestroyMaterial(Material material);

        public void LoadGeometry(Geometry geometry, Vertex3d[] vertices, uint[] indices);
        public void DestroyGeometry(Geometry geometry);
    }
}
