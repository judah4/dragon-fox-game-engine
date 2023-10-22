using Silk.NET.Maths;

namespace DragonGameEngine.Core.Rendering
{
    public readonly struct RenderSystemState
    {
        public readonly Matrix4X4<float> Projection;
        public readonly Matrix4X4<float> View;
        public readonly float NearClipPlane;
        public readonly float FarClipPlane;

        public readonly Matrix4X4<float> UiProjection;
        public readonly Matrix4X4<float> UiView;

        public RenderSystemState(Matrix4X4<float> projection, Matrix4X4<float> view, float nearClipPlane, float farClipPlane, Matrix4X4<float> uiProjection, Matrix4X4<float> uiView)
        {
            Projection = projection;
            View = view;
            NearClipPlane = nearClipPlane;
            FarClipPlane = farClipPlane;
            UiProjection = uiProjection;
            UiView = uiView;
        }

        public RenderSystemState UpdateWorldView(Matrix4X4<float> view)
        {
            return new RenderSystemState(Projection, view, NearClipPlane, FarClipPlane, UiProjection, UiView);
        }
    }
}
