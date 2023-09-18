using Silk.NET.Windowing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DragonFoxGameEngine.Core;

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
            builder.Configuration.AddUserSecrets<Program>();
            builder.Services.AddHostedService<EngineHostedService>();

            IHost host = builder.Build();

            var configuration = host.Services.GetRequiredService<IConfiguration>();

            host.Run();
        }
    }
}
