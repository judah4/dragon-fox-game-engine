using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using Silk.NET.Maths;
using System.Drawing;

namespace GameEngine.Core.Tests.Mocks
{
    public class MockRenderer : IRenderer
    {

        public Action? OnInit { get; set; }
        public Func<string, Vector2D<uint>, byte, byte[], bool, InnerTexture>? OnCreateTexture { get; set; }

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

        public InnerTexture CreateTexture(string name, Vector2D<uint> size, byte channelCount, Span<byte> pixels, bool hasTransparency)
        {
            if (OnCreateTexture == null)
            {
                return default;
            }
            return OnCreateTexture(name, size, channelCount, pixels.ToArray(), hasTransparency);
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
