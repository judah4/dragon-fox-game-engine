using DragonGameEngine.Core.Platforms;
using DragonGameEngine.Core.Rendering;
using Microsoft.Extensions.Logging;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;

namespace DragonGameEngine.Core
{
    public class GameApplication
    {
        private readonly ApplicationConfig _config;
        private readonly IGameEntry _game;
        private readonly IWindow _window;
        private readonly RendererFrontend _renderer;
        private readonly ILogger _logger;
        private readonly EngineInternalInput _engineInternalInput;

        private long _frame;

        //Debug fps stuff
        private readonly TimeSpan _fpsDisplayTime = TimeSpan.FromSeconds(0.1);
        private readonly TimeSpan _fpsFrameStatsTime = TimeSpan.FromSeconds(10.0);
        private readonly FrameStats _frameStats;
        private DateTime _lastFpsTime = DateTime.UtcNow;
        private DateTime _lastFpsFrameStatsTime = DateTime.UtcNow;

        public GameApplication(ApplicationConfig config, IGameEntry game, IWindow window, ILogger logger, RendererFrontend rendererFrontend)
        {
            _config = config;
            _game = game;
            _window = window;
            _logger = logger;

            _window.Update += OnUpdate;
            _window.Render += OnDrawFrame;
            _window.Resize += OnResize;

            _renderer = rendererFrontend;

            IInputContext input = window!.CreateInput();
            _engineInternalInput = new EngineInternalInput(input, window, logger);
            _frameStats = new FrameStats();
        }

        public GameApplication(ApplicationConfig config, IGameEntry game, IWindow window, ILogger logger) : this(config, game, window, logger, new RendererFrontend(config, window, logger))
        {
        }

        public void Init()
        {
            _renderer.Init();
            _game.Initialize(_window);
        }

        private void OnResize(Vector2D<int> size)
        {
            var unsignedSize = new Vector2D<uint>((uint)size.X, (uint)size.Y);
            _renderer.Resized(unsignedSize);
            _game.OnResize(unsignedSize);
        }

        public void Run()
        {
            _window.Run();
        }

        public void Shutdown()
        {
            _game.Shutdown();
            _renderer.Shutdown();
        }

        private void OnUpdate(double deltaTime)
        {
            _frameStats.AddSample(_frame, _window.Time, deltaTime);
            if (_lastFpsTime < DateTime.UtcNow.Add(-_fpsDisplayTime))
            {
                _window.Title = $"{_config.Title} ({_frameStats.GetCurrentFps().ToString("F0").PadLeft(4, '0')}) - ({deltaTime.ToString().PadLeft(9, '0')} s) - Min: {_frameStats.GetMinFps().ToString("F0")}, Max: {_frameStats.GetMaxFps().ToString("F0")}, 95th: {_frameStats.GetPercentile95thTime()} s";
                _lastFpsTime = DateTime.UtcNow;
            }

            if (_lastFpsFrameStatsTime < DateTime.UtcNow.Add(-_fpsFrameStatsTime))
            {
                _frameStats.SendDebugMessage(_logger);
                _lastFpsFrameStatsTime = DateTime.UtcNow;
            }

            _game.Update(deltaTime);
        }

        public void OnDrawFrame(double deltaTime)
        {
            _game.Render(deltaTime);

            var renderPacket = new RenderPacket(deltaTime);

            _renderer.DrawFrame(renderPacket);

            //end, to the next frame
            _frame++;
        }
    }
}
