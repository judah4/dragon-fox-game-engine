using Silk.NET.Windowing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DragonFoxGameEngine.Core;
using System;
using System.IO;

namespace DragonFoxGameEngine
{
    internal class Program
    {
        private static void Main(string[] args)
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
            builder.Configuration.AddUserSecrets<Program>();
            builder.Services.AddHostedService<EngineHostedService>();

            IHost host = builder.Build();

            host.Run();
        }
    }
}
