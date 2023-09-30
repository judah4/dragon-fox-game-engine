using Microsoft.Extensions.Logging;
using Silk.NET.Maths;

namespace DragonFoxGameEngine.Core.Rendering.Headless
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

        public bool EndFrame(double deltaTime)
        {
            return true;
        }

        public void Resized(Vector2D<uint> size)
        {
        }

        public void Shutdown()
        {
        }
    }
}
