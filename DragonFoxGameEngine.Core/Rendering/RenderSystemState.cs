using Silk.NET.Maths;

namespace DragonGameEngine.Core.Rendering
{
    public readonly struct RenderSystemState
    {
        public readonly Matrix4X4<float> Projection;
        public readonly Matrix4X4<float> View;
        public readonly float NearClipPlane;
        public readonly float FarClipPlane;

        public RenderSystemState(Matrix4X4<float> projection, Matrix4X4<float> view, float nearClipPlane, float farClipPlane)
        {
            Projection = projection;
            View = view;
            NearClipPlane = nearClipPlane;
            FarClipPlane = farClipPlane;
        }

        public RenderSystemState UpdateView(Matrix4X4<float> view)
        {
            return new RenderSystemState(Projection, view, NearClipPlane, FarClipPlane);
        }
    }
}
