using DragonGameEngine.Core.Platforms;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using Microsoft.Extensions.Logging;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;
using System.Collections.Immutable;

namespace DragonGameEngine.Core
{
    public sealed class GameApplication
    {
        public IWindow Window => _window;
        public IRendererFrontend Renderer => _renderer;

        private readonly ApplicationConfig _config;
        private readonly IGameEntry _game;
        private readonly IWindow _window;
        private readonly IRendererFrontend _renderer;
        private readonly ILogger _logger;
        private readonly EngineInternalInput _engineInternalInput;
        private readonly TextureSystem _textureSystem;
        private readonly MaterialSystem _materialSystem;
        private readonly GeometrySystem _geometrySystem;
        private readonly ResourceSystem _resourceSystem;

        private long _frame;
        private string _gameTitleData = string.Empty;

        //Debug fps stuff
        private readonly TimeSpan _fpsDisplayTime = TimeSpan.FromSeconds(0.5);
        private readonly TimeSpan _fpsFrameStatsTime = TimeSpan.FromSeconds(10.0);
        private readonly FrameStats _frameStats;
        private DateTime _lastFpsTime = DateTime.UtcNow;
        private DateTime _lastFpsFrameStatsTime = DateTime.UtcNow;

        //TODO: temp geometry
        private Geometry? _testGeometry;
        private int _testChoice;

        private Geometry? _testGeometry2;

        private Geometry? _cubeGeometry;


        public GameApplication(ApplicationConfig config, IGameEntry game, IWindow window, ILogger logger, IRendererFrontend rendererFrontend, 
            TextureSystem textureSystem, MaterialSystem materialSystem, GeometrySystem geometrySystem, ResourceSystem resourceSystem)
        {
            _config = config;
            _game = game;
            _window = window;
            _logger = logger;

            _window.Update += OnUpdate;
            _window.Render += OnDrawFrame;
            _window.Resize += OnResize;

            _renderer = rendererFrontend;
            _textureSystem = textureSystem;
            _materialSystem = materialSystem;
            _geometrySystem = geometrySystem;

            IInputContext input = window!.CreateInput();
            _engineInternalInput = new EngineInternalInput(input, window, logger);
            _frameStats = new FrameStats();
            _resourceSystem = resourceSystem;
        }

        public void Init()
        {
            _resourceSystem.Init();

            _renderer.Init();

            _textureSystem.Init(_renderer);

            _materialSystem.Init(_renderer);

            _geometrySystem.Init(_renderer);

            // TODO: temp 

            // Load up a plane configuration, and load geometry from it.
            var geometryConfig = _geometrySystem.GeneratePlaneConfig(10.0f, 5.0f, 5, 5, 5.0f, 2.0f, "test geometry", "test_material");
            _testGeometry = _geometrySystem.AcquireFromConfig(geometryConfig, true);

            // Load up default geometry.
            //_testGeometry = _geometrySystem.GetDefaultGeometry();

            var geometryConfig2 = _geometrySystem.GeneratePlaneConfig(10.0f, 10.0f, 5, 5, 5.0f, 5.0f, "test flat plane", "test_plane_mat");
            _testGeometry2 = _geometrySystem.AcquireFromConfig(geometryConfig2, true);

            _cubeGeometry = _geometrySystem.Acquire("SpinnyBlobs/model/tiny_blobfox.glb");

            // TODO: end temp 

            _game.Initialize(this);
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
            try
            {
                _game.Shutdown();
            }
            catch(Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            _geometrySystem.Shutdown();
            _materialSystem.Shutdown();
            _textureSystem.Shutdown();
            _renderer.Shutdown();
            _resourceSystem.Shutdown();

        }

        private void OnUpdate(double deltaTime)
        {
            _frameStats.AddSample(_frame, _window.Time, deltaTime);
            if (_lastFpsTime < DateTime.UtcNow.Add(-_fpsDisplayTime))
            {
                _window.Title = $"{_config.Title} ({_frameStats.GetCurrentFps().ToString("F0").PadLeft(4, '0')}) - ({deltaTime.ToString().PadLeft(9, '0')} s) - Min: {_frameStats.GetMinFps().ToString("F0")}, Max: {_frameStats.GetMaxFps().ToString("F0")}, 95th: {_frameStats.GetPercentile95thTime()} s {_gameTitleData}";
                _lastFpsTime = DateTime.UtcNow;
            }

            if (_lastFpsFrameStatsTime < DateTime.UtcNow.Add(-_fpsFrameStatsTime))
            {
                _frameStats.SendDebugMessage(_logger);
                _lastFpsFrameStatsTime = DateTime.UtcNow;
            }

            try
            {
                _game.Update(deltaTime);
            }
            catch(Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public void OnDrawFrame(double deltaTime)
        {
            _game.Render(deltaTime);

            var geometries = ImmutableArray<GeometryRenderData>.Empty;

            if(_testGeometry == null || _testGeometry2 == null || _cubeGeometry == null)
            {
                _logger.LogWarning("Expected test geometry to exist.");
            }
            else
            {
                geometries = ImmutableArray.Create(new GeometryRenderData()
                {
                    Geometry = _testGeometry,
                    Model = Matrix4X4.CreateTranslation(new Vector3D<float>(0, 3.5f, -5)),
                },
                new GeometryRenderData()
                {
                    Geometry = _testGeometry2,
                    Model = Matrix4X4.CreateFromAxisAngle(Vector3D<float>.UnitX, -MathF.PI/2f),
                },
                new GeometryRenderData()
                {
                    Geometry = _cubeGeometry,
                    Model = Matrix4X4.CreateTranslation(new Vector3D<float>(0, 1.5f, 0)),
                });
            }

            var renderPacket = new RenderPacket(deltaTime, geometries);

            _renderer.DrawFrame(renderPacket);

            //end, to the next frame
            _frame++;
        }

        public void UpdateWindowTitle(string title)
        {
            _gameTitleData = title;
        }

        /// <summary>
        /// Cycle the square's texture for texture loading
        /// </summary>
        public void CycleTestTexture()
        {
            if (_textureSystem == null)
            {
                _logger.LogError("Renderer not initialized");
                return;
            }
            if (_testGeometry == null)
            {
                _logger.LogError("game not initialized");
                return;
            }

            var textureNames = new string[]
            {
                "cobblestone",
                "paving",
                "paving2",
                "CoffeeDragon",
            };

            var oldName = textureNames[_testChoice];

            _testChoice++;
            _testChoice %= textureNames.Length;
            Texture texture;
            try
            {
                texture = _textureSystem.Acquire(textureNames[_testChoice], true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"No texture to load, using default - {e.Message}");
                texture = _textureSystem.GetDefaultTexture();
            }

            _testGeometry.Material.UpdateMetaData(_testGeometry.Material.DiffuseColor, new TextureMap()
            {
                Texture = texture,
                TextureUse = TextureUse.MapDiffuse,
            });

            _textureSystem.Release(oldName);
        }

    }
}
