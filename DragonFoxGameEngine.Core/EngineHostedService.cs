using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DragonFoxGameEngine.Core
{
    public class EngineHostedService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        IHostApplicationLifetime _appLifetime;

        public EngineHostedService(
            IConfiguration configuration,
            ILogger<EngineHostedService> logger,
            IHostApplicationLifetime appLifetime)
        {
            _configuration = configuration;
            _logger = logger;
            _appLifetime = appLifetime;

            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("1. StartAsync has been called.");
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _logger.LogInformation("2. OnStarted has been called.");
            var app = new HelloTriangleApplication(_logger);
            app.Run();
            _appLifetime.StopApplication();
        }

        private void OnStopping()
        {
            _logger.LogInformation("3. OnStopping has been called.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("4. StopAsync has been called.");

            return Task.CompletedTask;
        }

    }
}