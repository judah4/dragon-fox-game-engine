using DragonFoxGameEngine.Core.Rendering.Vulkan;
using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;

namespace DragonFoxGameEngine.Core.Rendering
{
    public sealed class RendererBackend : IRenderer
    {
        private readonly RendererBackendType _type;
        private readonly IWindow _window;
        private readonly string _applicationName;
        private readonly ILogger _logger;

        IRenderer _renderer;
        VulkanContext _context;
        public RendererBackend(RendererBackendType type, string applicationName, IWindow window, ILogger logger)
        {
            _type = type;
            _applicationName = applicationName;
            _window = window;
            _logger = logger;

            if(type == RendererBackendType.Vulkan)
            {
                var renderer = new VulkanBackendRenderer(logger);
                _context = renderer.Init(_applicationName, _window);
                _renderer = renderer;
            }
            else
            {
                throw new Exception($"Renderer {type} could not be setup!");
            }
           
        }

        public void Destroy()
        {
            _renderer.Shutdown();
        }

        public void Resized(Vector2D<uint> size)
        {
            _renderer.Resized(size);
        }

        public void BeginFrame(double deltaTime)
        {
            _renderer.BeginFrame(deltaTime);
        }

        public void EndFrame(double deltaTime)
        {
            _renderer.EndFrame(deltaTime);
        }
    }
}
