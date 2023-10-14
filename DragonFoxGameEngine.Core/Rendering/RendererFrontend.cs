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
        private float rotationAngle = 0f;

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
                _rendererBackend.UpdateGlobalState(_systemState.Projection, _systemState.View, Vector3D<float>.Zero, Color.White, 0);

                rotationAngle += 1f * (float)packet.DeltaTime;
                var rotation = Quaternion<float>.CreateFromAxisAngle(new Vector3D<float>(0, 0, 1), rotationAngle);
                // model is the object's matrix. Postion, rotation, and scale
                var model = Matrix4X4.CreateFromQuaternion(rotation);
                _rendererBackend.UpdateObject(model);

                _rendererBackend.EndFrame(packet.DeltaTime);
            }
        }

        public void SetView(Matrix4X4<float> view)
        {
            _systemState = _systemState.UpdateView(view);
        }

        private RenderSystemState RegenProjectionMatrix(float nearClip, float farClip)
        {
            var projection = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45f), (float)_window.Size.X / (float)_window.Size.Y, nearClip, farClip);
            var renderSystemState = new RenderSystemState(projection, _systemState.View, farClip, nearClip);
            return renderSystemState;
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
