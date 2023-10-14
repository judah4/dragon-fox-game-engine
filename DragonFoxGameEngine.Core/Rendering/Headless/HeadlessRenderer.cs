using DragonGameEngine.Core.Resources;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using System;
using System.Drawing;

namespace DragonGameEngine.Core.Rendering.Headless
{
    public class HeadlessRenderer : IRenderer
    {
        private readonly ILogger _logger;
        private Texture _defaultTexture;

        public Texture DefaultDiffuse => _defaultTexture;

        public HeadlessRenderer(ILogger logger)
        {
            _logger = logger;
        }

        public void Init(Texture defaultTexture)
        {
            _defaultTexture = defaultTexture;
            _logger.LogInformation("Headless Renderer setup.");
        }

        public bool BeginFrame(double deltaTime)
        {
            return true;
        }

        public void UpdateGlobalState(Matrix4X4<float> projection, Matrix4X4<float> view, Vector3D<float> viewPosition, Color ambientColor, int mode)
        {
            throw new System.NotImplementedException();
        }

        public void EndFrame(double deltaTime)
        {
        }

        public void Resized(Vector2D<uint> size)
        {
        }

        public void Shutdown()
        {
        }

        public void UpdateObject(GeometryRenderData data)
        {
            //might want to use this for interest area later
        }

        public InnerTexture CreateTexture(string name, bool autoRelease, Vector2D<uint> size, byte channelCount, Span<byte> pixels, bool hasTransparency)
        {
            return new InnerTexture(size, channelCount, hasTransparency, new object());
        }

        public void DestroyTexture(Texture texture)
        {
            texture.ResetGeneration();
        }
    }
}
