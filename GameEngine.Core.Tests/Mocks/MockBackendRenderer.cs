using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using Silk.NET.Maths;
using System.Drawing;

namespace GameEngine.Core.Tests.Mocks
{
    public class MockBackendRenderer : IRenderer
    {

        public Action? OnInit { get; set; }
        public Action? OnShutdown { get; set; }

        public Action<byte[], Texture>? OnLoadTexture { get; set; }
        public Action<Material>? OnLoadMaterial { get; set; }
        public Action<Material>? OnDestroyMaterial { get; set; }

        public Action<Geometry>? OnLoadGeometry { get; set; }
        public Action<Geometry>? OnDestroyGeometry { get; set; }

        public void Init()
        {
            OnInit?.Invoke();
        }

        public bool BeginFrame(double deltaTime)
        {
            throw new NotImplementedException();
        }

        public void LoadTexture(Span<byte> pixels, Texture texture)
        {
            if (OnLoadTexture == null)
            {
                return;
            }
            OnLoadTexture(pixels.ToArray(), texture);
        }

        public void DestroyTexture(Texture texture)
        {
        }

        public void EndFrame(double deltaTime)
        {
            throw new NotImplementedException();
        }

        public bool BeginRenderpass(RenderpassId renderpassId)
        {
            throw new NotImplementedException();
        }

        public void EndRenderpass(RenderpassId renderpassId)
        {
            throw new NotImplementedException();
        }

        public void Resized(Vector2D<uint> size)
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            OnShutdown?.Invoke();
        }

        public void UpdateGlobalWorldState(Matrix4X4<float> projection, Matrix4X4<float> view, Vector3D<float> viewPosition, Color ambientColor, int mode)
        {
            throw new NotImplementedException();
        }


        public void UpdateGlobalUiState(Matrix4X4<float> projection, Matrix4X4<float> view, int mode)
        {
            throw new NotImplementedException();
        }

        public void DrawGeometry(GeometryRenderData data)
        {
        }

        public void LoadMaterial(Material material)
        {
            OnLoadMaterial?.Invoke(material);
        }

        public void DestroyMaterial(Material material)
        {
            OnDestroyMaterial?.Invoke(material);
        }

        public void LoadGeometry(Geometry geometry, Vertex3d[] vertices, uint[] indices)
        {
            OnLoadGeometry?.Invoke(geometry);
        }

        public void DestroyGeometry(Geometry geometry)
        {
            OnDestroyGeometry?.Invoke(geometry);
        }
    }
}
