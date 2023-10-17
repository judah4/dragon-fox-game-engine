using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using Silk.NET.Maths;
using System.Drawing;

namespace GameEngine.Core.Tests.Mocks
{
    public class MockRenderer : IRenderer
    {

        public Action? OnInit { get; set; }
        public Action<byte[], Texture>? OnLoadTexture { get; set; }

        public void Init()
        {
            if(OnInit == null)
            {
                return;
            }
            OnInit();
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

        public void Resized(Vector2D<uint> size)
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
        }

        public void UpdateGlobalState(Matrix4X4<float> projection, Matrix4X4<float> view, Vector3D<float> viewPosition, Color ambientColor, int mode)
        {
            throw new NotImplementedException();
        }

        public void UpdateObject(GeometryRenderData data)
        {
        }
    }
}
