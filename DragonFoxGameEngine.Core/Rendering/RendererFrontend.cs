using DragonGameEngine.Core.Platforms;
using DragonGameEngine.Core.Rendering.Headless;
using DragonGameEngine.Core.Rendering.Vulkan;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System.Drawing;

namespace DragonGameEngine.Core.Rendering
{
    public sealed class RendererFrontend
    {
        private readonly IWindow _window;
        private readonly ApplicationConfig _config;

        private readonly ILogger _logger;
        private readonly IRenderer _rendererBackend;
        private float posZ = -1.0f;

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
            if (_rendererBackend.BeginFrame(packet.DeltaTime))
            {
                var projection = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45f), (float)1280f / 720, 0.1f, 1000.0f);
                posZ += -1.0f * (float)packet.DeltaTime;
                var view = Matrix4X4.CreateTranslation(new Vector3D<float>(0, 0, posZ));
                //var view = Matrix4X4.CreateLookAt(CameraPosition, CameraPosition + CameraFront, CameraUp);
                _rendererBackend.UpdateGlobalState(projection, view, Vector3D<float>.Zero, Color.White, 0);

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
