using DragonFoxGameEngine.Core.Platforms;
using DragonFoxGameEngine.Core.Rendering.Headless;
using DragonFoxGameEngine.Core.Rendering.Vulkan;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.SDL;
using Silk.NET.Windowing;

namespace DragonFoxGameEngine.Core.Rendering
{
    public sealed class RendererFrontend
    {
        private readonly IWindow _window;
        private readonly ApplicationConfig _config;

        private readonly ILogger _logger;
        private readonly IRenderer _rendererBackend;

        public RendererFrontend(ApplicationConfig config, IWindow window, ILogger logger, IRenderer renderer)
        {
            _config = config;
            _window = window;
            _logger = logger;
            _rendererBackend = renderer;
        }

        public RendererFrontend(ApplicationConfig config, IWindow window, ILogger logger)
            : this(config, window, logger, SetupRenderer(config, window, logger))
        {
        }

        public void Init()
        {
            _rendererBackend.Init();
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

        private static IRenderer SetupRenderer(ApplicationConfig config, IWindow window, ILogger logger)
        {
            IRenderer renderer;
            if (!config.HeadlessMode)
            {
                renderer = new VulkanBackendRenderer(config.Title, window, logger);
            }
            else
            {
                renderer = new HeadlessRenderer(logger);
            }


            return new RendererBackend(window, logger, renderer);
        }

    }
}
