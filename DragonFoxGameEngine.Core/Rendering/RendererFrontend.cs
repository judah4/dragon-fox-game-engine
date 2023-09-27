using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace DragonFoxGameEngine.Core.Rendering
{
    public sealed class RendererFrontend
    {
        private readonly IWindow _window;
        private readonly string _applicationName;

        private readonly ILogger _logger;
        private readonly RendererBackend _rendererBackend;

        public RendererFrontend(string applicationName, IWindow window, ILogger logger)
        {
            _applicationName = applicationName;
            _window = window;
            _logger = logger;

            _rendererBackend = new RendererBackend(RendererBackendType.Vulkan, applicationName, window, logger);
        }

        public void Shutdown()
        {
            _rendererBackend.Shutdown();
        }

        public void Resized(Vector2D<uint> size)
        {
            _rendererBackend.Resized(size);
        }

        public void DrawFrame(RenderPacket packet)
        {
            if(_rendererBackend.BeginFrame(packet.DeltaTime))
            {
                _rendererBackend.EndFrame(packet.DeltaTime);
            }

        }
    }
}
