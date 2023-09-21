using Silk.NET.Windowing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DragonFoxGameEngine.Core;
using System;
using System.IO;
using DragonFoxGameEngine.Core.Platforms;
using DragonFoxGameEngine.Game;

namespace DragonFoxGameEngine
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var host = SetupHost(args);
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Engine");

            var config = new ApplicationConfig();

            var platform = new PlatformWindowing(logger);
            var window = platform.InitWindow(config, null);

            //Initialize game logic here
            IGame game = new Game.GameEntry();

            ApplicationRun(config, platform, window, game, logger);
        }

        static void ApplicationRun(ApplicationConfig config, PlatformWindowing platform, IWindow window, IGame game, ILogger logger)
        {
            var application = new GameApplication(config, game, window, logger);

            application.Run();

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
