using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using DragonGameEngine.Core.Resources;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;
using System.Drawing;

namespace DragonGameEngine.Core.Rendering
{
    /// <summary>
    /// The backend renderer
    /// </summary>
    /// <remarks>
    /// Can probably be simplified later and removed.
    /// </remarks>
    public sealed class RendererBackend : IRenderer
    {
        private readonly IWindow _window;
        private readonly ILogger _logger;
        private readonly IRenderer _renderer;

        public Texture DefaultDiffuse => _renderer.DefaultDiffuse;

        public RendererBackend(IWindow window, ILogger logger, IRenderer renderer)
        {
            _window = window;
            _logger = logger;
            _renderer = renderer;
        }

        public void Init(Texture defaultTexture)
        {
            _renderer.Init(defaultTexture);
        }

        public void Shutdown()
        {
            _renderer.Shutdown();
        }

        public void Resized(Vector2D<uint> size)
        {
            _renderer.Resized(size);
        }

        public bool BeginFrame(double deltaTime)
        {
            return _renderer.BeginFrame(deltaTime);
        }

        public void UpdateGlobalState(Matrix4X4<float> projection, Matrix4X4<float> view, Vector3D<float> viewPosition, Color ambientColor, int mode)
        {
            _renderer.UpdateGlobalState(projection, view, viewPosition, ambientColor, mode);
        }

        public void EndFrame(double deltaTime)
        {
            _renderer.EndFrame(deltaTime);
        }

        public void UpdateObject(GeometryRenderData data)
        {
            _renderer.UpdateObject(data);
        }

        public InnerTexture CreateTexture(string name, bool autoRelease, Vector2D<uint> size, byte channelCount, Span<byte> pixels, bool hasTransparency)
        {
            return _renderer.CreateTexture(name, autoRelease, size, channelCount, pixels, hasTransparency);
        }

        public void DestroyTexture(Texture texture)
        {
            _renderer.DestroyTexture(texture);
        }
    }
}
