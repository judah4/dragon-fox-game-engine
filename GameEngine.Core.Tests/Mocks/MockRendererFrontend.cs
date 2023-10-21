using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using Silk.NET.Maths;

namespace GameEngine.Core.Tests.Mocks
{
    public class MockRendererFrontend : IRendererFrontend
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

        public void DrawFrame(RenderPacket packet)
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

        public void LoadTexture(Span<byte> pixels, Texture texture)
        {
            OnLoadTexture?.Invoke(pixels.ToArray(), texture);
        }

        public void DestroyTexture(Texture texture)
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

        public void SetView(Matrix4X4<float> view)
        {
            throw new NotImplementedException();
        }
    }
}
