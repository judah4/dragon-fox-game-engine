using Silk.NET.Windowing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DragonFoxGameEngine.Core;
using System;

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
            //Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create)
            //https://github.com/nreco/logging
            builder.Logging.AddFile("app.log", fileLoggerOpts => {
                fileLoggerOpts.Append = false;
                //fileLoggerOpts.MinLevel = LogLevel.Information;
                fileLoggerOpts.FormatLogEntry = (msg) => {
                    var sb = new System.Text.StringBuilder();
                    sb.Append(DateTime.Now.ToString("o"));
                    sb.Append(" ");
                    sb.Append(msg.LogLevel);
                    sb.Append(" ");
                    sb.Append(msg.LogName);
                    sb.Append(" ");
                    sb.Append(msg.EventId.Id);
                    sb.Append(" ");
                    sb.Append(msg.Message);
                    sb.Append(" ");
                    sb.Append(msg.Exception?.ToString());
                    sb.Append(" ");
                    return sb.ToString();
                };
            });
            builder.Configuration.AddUserSecrets<Program>();
            builder.Services.AddHostedService<EngineHostedService>();

            IHost host = builder.Build();

            host.RunAsync();

            //var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            //var logger = loggerFactory.CreateLogger("Engine");
            //var app = new HelloTriangleApplication(logger);
            //app.Run();
        }
    }
}
