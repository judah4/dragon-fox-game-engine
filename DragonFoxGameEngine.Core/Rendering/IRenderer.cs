using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Resources;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System;
using System.Drawing;

namespace DragonGameEngine.Core.Rendering
{
    public interface IRenderer
    {
        /// <summary>
        /// Init renderer
        /// </summary>
        public void Init();
        public void Shutdown();

        public void Resized(Vector2D<uint> size);

        public bool BeginFrame(double deltaTime);

        public void UpdateGlobalWorldState(Matrix4X4<float> projection, Matrix4X4<float> view, Vector3D<float> viewPosition, Color ambientColor, int mode);
        public void UpdateGlobalUiState(Matrix4X4<float> projection, Matrix4X4<float> view, int mode);

        public void EndFrame(double deltaTime);

        public bool BeginRenderpass(RenderpassId renderpassId);

        public void EndRenderpass(RenderpassId renderpassId);

        /// <summary>
        /// Loads the pixels into the texture
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="texture"></param>
        /// <remarks>
        /// This is create texture in the kohi tutorial series
        /// </remarks>
        public void LoadTexture(Span<byte> pixels, Texture texture);

        public void DestroyTexture(Texture texture);
        public void LoadMaterial(Material material);
        public void DestroyMaterial(Material material);

        public void LoadGeometry(Geometry geometry, Vertex3d[] vertices, uint[] indices);
        public void DestroyGeometry(Geometry geometry);
        public void DrawGeometry(GeometryRenderData data);
    }
}
