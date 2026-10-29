using System;
using System.Drawing;
using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace DragonFox.Rendering
{
    public sealed class RendererFrontend : IRendererFrontend
    {
        private readonly IWindow _window;

        private readonly ILogger _logger;

        private readonly IRenderer _rendererBackend;

        private RenderSystemState _systemState;

        public RendererFrontend(
            IWindow window,
            ILogger logger,
            IRenderer renderer)
        {
            _window = window;
            _logger = logger;
            _rendererBackend = renderer;
        }

        public void Init()
        {
            _rendererBackend.Init();

            var initialView = Matrix4X4.CreateTranslation(new Vector3D<float>(0, 0, 10));
            Matrix4X4.Invert(initialView, out initialView);

            _systemState = RegenProjectionMatrix(0.1f, 2000.0f)
                .UpdateWorldView(initialView);
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
                if (!_rendererBackend.BeginRenderpass(RenderpassId.World))
                {
                    _logger.LogError("Could not being render pass for World");
                    return;
                }

                _rendererBackend.UpdateGlobalWorldState(_systemState.Projection, _systemState.View,
                    Vector3D<float>.Zero, Color.White, 0);

                for (int cnt = 0; cnt < packet.Geometries.Count; cnt++)
                {
                    _rendererBackend.DrawGeometry(packet.Geometries[cnt]);
                }

                _rendererBackend.EndRenderpass(RenderpassId.World);

                if (!_rendererBackend.BeginRenderpass(RenderpassId.Ui))
                {
                    _logger.LogError("Could not being render pass for UI");
                    return;
                }

                _rendererBackend.UpdateGlobalUiState(_systemState.UiProjection, _systemState.UiView, 0);

                for (int cnt = 0; cnt < packet.UiGeometries.Count; cnt++)
                {
                    _rendererBackend.DrawGeometry(packet.UiGeometries[cnt]);
                }

                _rendererBackend.EndRenderpass(RenderpassId.Ui);

                _rendererBackend.EndFrame(packet.DeltaTime);
            }
        }

        public void SetView(Matrix4X4<float> view)
        {
            _systemState = _systemState.UpdateWorldView(view);
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
            var projection = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45f),
                (float)_window.Size.X / (float)_window.Size.Y, nearClip, farClip);

            //ui projection /view
            var uiProjection =
                Matrix4X4.CreateOrthographicOffCenter(0, (float)_window.Size.X, (float)_window.Size.Y, 0, -100f, 100f);

            var uiView = Matrix4X4<float>.Identity;
            Matrix4X4.Invert(uiView, out uiView);

            var renderSystemState =
                new RenderSystemState(projection, _systemState.View, farClip, nearClip, uiProjection, uiView);
            return renderSystemState;
        }
    }
}
