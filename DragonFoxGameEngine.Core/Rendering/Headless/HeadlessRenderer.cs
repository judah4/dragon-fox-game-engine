using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using System.Drawing;

namespace DragonGameEngine.Core.Rendering.Headless
{
    public class HeadlessRenderer : IRenderer
    {
        public ILogger _logger;
        public HeadlessRenderer(ILogger logger)
        {
            _logger = logger;
        }

        public void Init()
        {
            _logger.LogInformation("Headless Renderer setup.");
        }

        public bool BeginFrame(double deltaTime)
        {
            return true;
        }

        public void UpdateGlobalState(Matrix4X4<float> projection, Matrix4X4<float> view, Vector3D<float> viewPosition, Color ambientColor, int mode)
        {
            throw new System.NotImplementedException();
        }

        public void EndFrame(double deltaTime)
        {
        }

        public void Resized(Vector2D<uint> size)
        {
        }

        public void Shutdown()
        {
        }

        public void UpdateObject(Matrix4X4<float> model)
        {
            //might want to use this for interest area later
        }

    }
}
