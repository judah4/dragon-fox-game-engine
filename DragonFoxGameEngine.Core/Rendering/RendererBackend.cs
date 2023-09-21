using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace DragonFoxGameEngine.Core.Rendering
{
    public class RendererBackend : IRenderer
    {
        private readonly RendererBackendType _type;
        private readonly IWindow _window;
        private readonly string _applicationName;
        private readonly ILogger _logger;

        public RendererBackend(RendererBackendType type, string applicationName, IWindow window, ILogger logger)
        {
            _type = type;
            _applicationName = applicationName;
            _window = window;
            _logger = logger;

            if(type == RendererBackendType.Vulkan)
            {

            }
            else
            {
                throw new Exception($"Renderer {type} could not be setup!");
            }
           
        }

        public void Destroy()
        {

        }

        public void Resized(Vector2D<int> size)
        {

        }

        public void BeginFrame(double deltaTime)
        {

        }

        public void EndFrame(double deltaTime)
        {

        }
    }
}
