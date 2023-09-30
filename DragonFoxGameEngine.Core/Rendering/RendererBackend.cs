using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace DragonFoxGameEngine.Core.Rendering
{
    public sealed class RendererBackend : IRenderer
    {
        private readonly IWindow _window;
        private readonly ILogger _logger;
        private readonly IRenderer _renderer;

        public RendererBackend(IWindow window, ILogger logger, IRenderer renderer)
        {
            _window = window;
            _logger = logger;
            _renderer = renderer;
        }

        public void Init()
        {
            _renderer.Init();
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

        public bool EndFrame(double deltaTime)
        {
            return _renderer.EndFrame(deltaTime);
        }
    }
}
