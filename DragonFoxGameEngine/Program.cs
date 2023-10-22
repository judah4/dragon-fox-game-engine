using Silk.NET.Windowing;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using Silk.NET.Maths;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Configuration;
using DragonGameEngine.Core.Platforms;
using DragonGameEngine.Core;
using System.Threading;
using DragonGameEngine.Core.Systems.Domain;
using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Systems;

namespace DragonFoxGameEngine
{
    internal class Program
    {
        private static string? _logPath;

        private static void Main(string[] args)
        {
            bool headlessMode = false;
            if (args.Any(x => x == "--headless"))
            {
                headlessMode = true;
            }
            var config = new ApplicationConfig(
                Game.GameEntry.GAME_NAME,
                new Vector2D<int>(-1, -1),
                new Vector2D<int>(ApplicationConfig.WIDTH, ApplicationConfig.HEIGHT),
                headlessMode,
                ApplicationConfig.GOOD_MAX_FPS,
                ApplicationConfig.GOOD_MAX_FPS);

            //var host = SetupHost(args);
            //var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            var loggerFactory = SetupLogger(config, args);
            var logger = loggerFactory.CreateLogger("Engine");
            //host.Run();
            //return;

            if (!string.IsNullOrEmpty(_logPath))
            {
                logger.LogDebug($"Logging file path: {_logPath}");
            }

            Thread.Sleep(1000); //reading the log delay

            var platform = new PlatformWindowing(logger);
            var window = platform.InitWindow(config, null);

            var gameLogger = loggerFactory.CreateLogger("Game");
            //Initialize game logic here
            IGameEntry game = new Game.GameEntry(gameLogger);

            ApplicationRun(config, platform, window, game, logger);
        }

        static void ApplicationRun(ApplicationConfig config, PlatformWindowing platform, IWindow window, IGameEntry game, ILogger engineLogger)
        {
            //for debugging, lets use the default path for now
            var resourceSystem = new ResourceSystem(engineLogger, new ResourceSystemConfig(32, "Assets"));

            var textureSystem = new TextureSystem(engineLogger, new TextureSystemConfig(65536), resourceSystem);

            var materialSystem = new MaterialSystem(engineLogger, new MaterialSystemConfig(4096), textureSystem, resourceSystem);
            var geometrySystem = new GeometrySystem(engineLogger, new GeometrySystemConfig(4096), materialSystem, resourceSystem);

            var rendererFrontend = new RendererFrontend(config, window, textureSystem, materialSystem, resourceSystem, engineLogger);
            var application = new GameApplication(config, game, window, engineLogger, rendererFrontend, textureSystem, materialSystem, geometrySystem, resourceSystem);

            try
            {
                application.Init();

                application.Run(); //Do the thing
            }
            catch (Exception e)
            {
                engineLogger.LogError(e, e.Message);
            }

            application.Shutdown();
            platform.Cleanup();
        }

        static ILoggerFactory SetupLogger(ApplicationConfig config, string[] args)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");
            var configuration = configurationBuilder.Build();
            var factory = LoggerFactory.Create(builder => {
                builder.ClearProviders()
                .AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConfiguration();
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                    options.ColorBehavior = LoggerColorBehavior.Enabled;
                });
                var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
                _logPath = Path.Combine(dataPath, $"dragonfox\\{config.Title}\\output.log");
                //https://github.com/nreco/logging
                builder.AddFile(_logPath, fileLoggerOpts => {
                    fileLoggerOpts.Append = false;
                    fileLoggerOpts.MinLevel = LogLevel.Debug; //TODO: turn this off eventually for publish
                    fileLoggerOpts.FormatLogEntry = LoggingOptions.FormatLogMessage;
                });
                //inline logging for debugging for now
                builder.AddFile("output.log", fileLoggerOpts => {
                    fileLoggerOpts.Append = false;
                    fileLoggerOpts.MinLevel = LogLevel.Debug; //TODO: turn this off eventually for publish
                    fileLoggerOpts.FormatLogEntry = LoggingOptions.FormatLogMessage;
                });
            });
            return factory;
        }
    }
}
