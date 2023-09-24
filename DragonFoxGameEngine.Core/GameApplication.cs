using DragonFoxGameEngine.Core.Platforms;
using DragonFoxGameEngine.Core.Rendering;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.SDL;
using Silk.NET.Windowing;
using System;

namespace DragonFoxGameEngine.Core
{
    public class GameApplication
    {
        private readonly ApplicationConfig _config;
        private readonly IGameEntry _game;
        private readonly IWindow _window;
        private readonly RendererFrontend _renderer;
        private readonly ILogger _logger;

        public GameApplication(ApplicationConfig config, IGameEntry game, IWindow window, ILogger logger)
        {
            _config = config;
            _game = game;
            _window = window;
            _logger = logger;

            _window.Update += OnUpdate;
            _window.Render += OnDrawFrame;
            _window.Resize += OnResize;

            _renderer = new RendererFrontend(config.Title, window, _logger);

            game.Initialize(window);
        }

        private void OnResize(Vector2D<int> size)
        {
            _renderer.Resized(size);
            _game.OnResize(size);
        }

        public void Run()
        {
            _window.Run();
        }

        public void Shutdown()
        {
            _renderer.Shutdown();
        }

        private void OnUpdate(double deltaTime)
        {
            var fps = (int)(1.0 / deltaTime);
            _window!.Title = $"{_config.Title} ({fps}) - ({deltaTime})";

            _game.Update(deltaTime);
        }

        public void OnDrawFrame(double deltaTime)
        {
            _game.Render(deltaTime);

            var renderPacket = new RenderPacket(deltaTime);

            _renderer.DrawFrame(renderPacket);
        }

    }
}
