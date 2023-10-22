using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Platforms;
using DragonGameEngine.Core.Rendering.Headless;
using DragonGameEngine.Core.Rendering.Vulkan;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;
using System.Drawing;

namespace DragonGameEngine.Core.Rendering
{
    public sealed class RendererFrontend : IRendererFrontend
    {
        private readonly ApplicationConfig _config;
        private readonly IWindow _window;
        private readonly TextureSystem _textureSystem;
        private readonly MaterialSystem _materialSystem;

        private readonly ILogger _logger;

        private readonly IRenderer _rendererBackend;

        private RenderSystemState _systemState;

        public RendererFrontend(ApplicationConfig config, IWindow window, TextureSystem textureSystem, MaterialSystem materialSystem, ILogger logger, IRenderer renderer)
        {
            _config = config;
            _window = window;
            _textureSystem = textureSystem;
            _materialSystem = materialSystem;
            _logger = logger;
            _rendererBackend = renderer;
        }

        public RendererFrontend(ApplicationConfig config, IWindow window, TextureSystem textureSystem, MaterialSystem materialSystem, ResourceSystem resourceSystem, ILogger logger)
            : this(config, window, textureSystem, materialSystem, logger, SetupRenderer(config, window, textureSystem, resourceSystem, logger))
        {
        }

        public void Init()
        {
            _rendererBackend.Init();

            var initialView = Matrix4X4.CreateTranslation(new Vector3D<float>(0, 0, 10));
            Matrix4X4.Invert(initialView, out initialView);

            _systemState = RegenProjectionMatrix(0.1f, 1000.0f)
                .UpdateView(initialView);
        }

        public void Shutdown()
        {
            _rendererBackend.Shutdown();
        }

        public void Resized(Vector2D<uint> size)
        {
            _systemState = RegenProjectionMatrix(_systemState.NearClipPlane, _systemState.FarClipPlane);
            _rendererBackend.Resized(size);
        }

        public void DrawFrame(RenderPacket packet)
        {
            if (_rendererBackend.BeginFrame(packet.DeltaTime))
            {
                _rendererBackend.UpdateGlobalState(_systemState.Projection, _systemState.View, Vector3D<float>.Zero, Color.White, 0);

                for(int cnt = 0; cnt < packet.Geometries.Count; cnt++)
                {
                    _rendererBackend.DrawGeometry(packet.Geometries[cnt]);
                }

                _rendererBackend.EndFrame(packet.DeltaTime);
            }
        }

        public void SetView(Matrix4X4<float> view)
        {
            _systemState = _systemState.UpdateView(view);
        }

        public void LoadTexture(Span<byte> pixels, Texture texture)
        {
            _rendererBackend.LoadTexture(pixels, texture);
        }

        public void DestroyTexture(Texture texture)
        {
            _rendererBackend.DestroyTexture(texture);
        }

        public void LoadMaterial(Material material)
        {
            _rendererBackend.LoadMaterial(material);
        }

        public void DestroyMaterial(Material material)
        {
            _rendererBackend.DestroyMaterial(material);
        }

        public void LoadGeometry(Geometry geometry, Vertex3d[] verticies, uint[] indicies)
        {
            _rendererBackend.LoadGeometry(geometry, verticies, indicies);
        }

        public void DestroyGeometry(Geometry geometry)
        {
            _rendererBackend.DestroyGeometry(geometry);
        }

        private RenderSystemState RegenProjectionMatrix(float nearClip, float farClip)
        {
            var projection = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45f), (float)_window.Size.X / (float)_window.Size.Y, nearClip, farClip);
            var renderSystemState = new RenderSystemState(projection, _systemState.View, farClip, nearClip);
            return renderSystemState;
        }

        private static IRenderer SetupRenderer(ApplicationConfig config, IWindow window, TextureSystem textureSystem, ResourceSystem resourceSystem, ILogger logger)
        {
            IRenderer renderer;
            if (!config.HeadlessMode)
            {
                renderer = new VulkanBackendRenderer(config.Title, window, textureSystem, resourceSystem, logger);
            }
            else
            {
                renderer = new HeadlessRenderer(logger);
            }

            return renderer;
        }
    }
}
