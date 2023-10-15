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

namespace DragonFoxGameEngine
{
    internal class Program
    {
        private static string? _logPath;

        private static void Main(string[] args)
        {
            //var host = SetupHost(args);
            //var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            var loggerFactory = SetupLogger(args);
            var logger = loggerFactory.CreateLogger("Engine");
            //host.Run();
            //return;

            if (!string.IsNullOrEmpty(_logPath))
            {
                logger.LogDebug($"Logging file path: {_logPath}");
            }

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

            var platform = new PlatformWindowing(logger);
            var window = platform.InitWindow(config, null);

            var gameLogger = loggerFactory.CreateLogger("Game");
            //Initialize game logic here
            IGameEntry game = new Game.GameEntry(gameLogger);

            ApplicationRun(config, platform, window, game, logger);
        }

        static void ApplicationRun(ApplicationConfig config, PlatformWindowing platform, IWindow window, IGameEntry game, ILogger logger)
        {
            var application = new GameApplication(config, game, window, logger);

            application.Init();

            application.Run(); //Do the thing

            application.Shutdown();
            platform.Cleanup();
        }

        static ILoggerFactory SetupLogger(string[] args)
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
                _logPath = Path.Combine(dataPath, "dragonfox/game1/output.log");
                //https://github.com/nreco/logging
                builder.AddFile(_logPath, fileLoggerOpts => {
                    fileLoggerOpts.Append = false;
                    fileLoggerOpts.MinLevel = LogLevel.Debug;
                    fileLoggerOpts.FormatLogEntry = LoggingOptions.FormatLogMessage;
                });
                //inline logging for debugging for now
                builder.AddFile("output.log", fileLoggerOpts => {
                    fileLoggerOpts.Append = false;
                    fileLoggerOpts.MinLevel = LogLevel.Debug;
                    fileLoggerOpts.FormatLogEntry = LoggingOptions.FormatLogMessage;
                });
            });
            return factory;
        }
    }
}
