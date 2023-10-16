using DragonGameEngine.Core.Resources;
using Silk.NET.Maths;
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

        public void UpdateGlobalState(Matrix4X4<float> projection, Matrix4X4<float> view, Vector3D<float> viewPosition, Color ambientColor, int mode);

        public void EndFrame(double deltaTime);

        public void UpdateObject(GeometryRenderData data);

        public InnerTexture CreateTexture(string name, Vector2D<uint> size, byte channelCount, Span<byte> pixels, bool hasTransparency);

        public void DestroyTexture(Texture texture);
    }
}
