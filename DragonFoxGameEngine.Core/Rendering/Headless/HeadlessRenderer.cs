using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using System;
using System.Drawing;

namespace DragonGameEngine.Core.Rendering.Headless
{
    public sealed class HeadlessRenderer : IRenderer
    {
        private readonly ILogger _logger;

        public HeadlessRenderer(ILogger logger)
        {
            _logger = logger;
        }

        public void Init()
        {
            _logger.LogInformation("Headless Renderer setup.");
        }

        public bool BeginFrame(double deltaTime)
        {
            return true;
        }

        public void UpdateGlobalWorldState(Matrix4X4<float> projection, Matrix4X4<float> view, Vector3D<float> viewPosition, Color ambientColor, int mode)
        {
        }

        public void UpdateGlobalUiState(Matrix4X4<float> projection, Matrix4X4<float> view, int mode)
        {
        }

        public void EndFrame(double deltaTime)
        {
        }


        public bool BeginRenderpass(RenderpassId renderpassId)
        {
            return true;
        }

        public void EndRenderpass(RenderpassId renderpassId)
        {
        }

        public void Resized(Vector2D<uint> size)
        {
        }

        public void Shutdown()
        {
        }

        public void DrawGeometry(GeometryRenderData data)
        {
            //might want to use this for interest area later
        }

        public void LoadTexture(Span<byte> pixels, Texture texture)
        {
            texture.UpdateTextureInternalData(true); //keep it simple for headless
        }

        public void DestroyTexture(Texture texture)
        {
            texture.ResetGeneration();
        }

        public void LoadMaterial(Material material)
        {
        }

        public void DestroyMaterial(Material material)
        {
        }

        public void LoadGeometry(Geometry geometry, Vertex3d[] vertices, uint[] indices)
        {
        }

        public void DestroyGeometry(Geometry geometry)
        {
        }

    }
}
