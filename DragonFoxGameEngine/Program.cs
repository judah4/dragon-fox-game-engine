using Silk.NET.Windowing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DragonFoxGameEngine.Core;
using System;
using System.IO;
using DragonFoxGameEngine.Core.Platforms;
using System.Linq;
using Silk.NET.Maths;
using Microsoft.Extensions.Logging.Console;

namespace DragonFoxGameEngine
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var host = SetupHost(args);
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Engine");
            //host.Run();
            //return;

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

        static IHost SetupHost(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            builder.Logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
                options.ColorBehavior = LoggerColorBehavior.Enabled;
            });
            var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            var logPath = Path.Combine(dataPath, "dragonfox/game1/output.log");
            //https://github.com/nreco/logging
#if DEBUG
            builder.Logging.AddFile("app.log", fileLoggerOpts => {
                fileLoggerOpts.Append = false;
                fileLoggerOpts.MinLevel = LogLevel.Debug;
                fileLoggerOpts.FormatLogEntry = LoggingOptions.FormatLogMessage;
            });
#endif
            builder.Logging.AddFile(logPath, fileLoggerOpts => {
                fileLoggerOpts.Append = false;
                fileLoggerOpts.MinLevel = LogLevel.Debug;
                fileLoggerOpts.FormatLogEntry = LoggingOptions.FormatLogMessage;
            });
            //builder.Services.AddHostedService<EngineHostedService>();

            IHost host = builder.Build();
            return host;
        }
    }
}
