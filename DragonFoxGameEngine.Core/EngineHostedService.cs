using Hardware.Info;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DragonFoxGameEngine.Core
{
    public class EngineHostedService : IHostedService
    {
        static readonly IHardwareInfo s_hardwareInfo = new HardwareInfo();

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

            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
            _logger.LogInformation($"Dragon Fox Game Engine {version}");

            s_hardwareInfo.RefreshAll();

            _logger.LogInformation(s_hardwareInfo.OperatingSystem.ToString());

            _logger.LogInformation(s_hardwareInfo.MemoryStatus.ToString());

            foreach (var hardware in s_hardwareInfo.BiosList)
                _logger.LogInformation(hardware.ToString());

            foreach (var cpu in s_hardwareInfo.CpuList)
            {
                _logger.LogInformation(cpu.ToString());

                foreach (var cpuCore in cpu.CpuCoreList)
                    _logger.LogInformation(cpuCore.ToString());
            }

            foreach (var hardware in s_hardwareInfo.KeyboardList)
                _logger.LogInformation(hardware.ToString());

            foreach (var hardware in s_hardwareInfo.MemoryList)
                _logger.LogInformation(hardware.ToString());

            foreach (var hardware in s_hardwareInfo.MonitorList)
                _logger.LogInformation(hardware.ToString());

            foreach (var hardware in s_hardwareInfo.MotherboardList)
                _logger.LogInformation(hardware.ToString());

            foreach (var hardware in s_hardwareInfo.VideoControllerList)
                _logger.LogInformation(hardware.ToString());

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