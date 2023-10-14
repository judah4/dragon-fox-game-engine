using DragonGameEngine.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Silk.NET.Core;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DragonGameEngine.Core.Platforms
{
    public class PlatformWindowing
    {
        private readonly ILogger _logger;

        private IWindow? _window;

        public PlatformWindowing(ILogger logger)
        {
            _logger = logger;
        }

        public unsafe IWindow InitWindow(ApplicationConfig config, Action? onLoadAction)
        {
            if (_window != null)
            {
                throw new EngineException("Window is already created!");
            }

            _logger.LogInformation("{gameEngineName} {engineVersion}", ApplicationInfo.GetGameEngineName(), ApplicationInfo.EngineVersion);
            _logger.LogInformation("{gameTitle} {gameVersion}", config.Title, ApplicationInfo.GameVersion);

            //Create a window.
            var options = WindowOptions.DefaultVulkan with
            {
                IsVisible = !config.HeadlessMode, //use IsVisible for setting up headless mode later
                Size = config.StartingSize,
                Title = config.Title,
                UpdatesPerSecond = config.UpdatesPerSecond,
                FramesPerSecond = config.FramesPerSecond,
                VSync = false,
            };

            if (config.StartingPos.X > 0 && config.StartingPos.Y > 0)
            {
                options.Position = config.StartingPos;
            }

            _window = Window.Create(options);
            _window.Load += onLoadAction;
            _window.Initialize();

            SetupIcon(_window);

            if (_window.VkSurface is null)
            {
                throw new EngineException("Windowing platform doesn't support Vulkan.");
            }

            _logger.LogInformation("Window initialized.");


            Task.Run(() =>
            {
                //this takes some time to pull hardware info so it's best to unload to another thread.
                _logger.LogInformation(HardwareStats.HardwareInfo());
            }).ConfigureAwait(true);

            return _window;
        }

        public void Cleanup()
        {
            _window?.Dispose();
        }

        private unsafe void SetupIcon(IWindow window)
        {
            var iconPath = "favicon.png";
            var fileExists = Path.Exists(iconPath);
            if (fileExists)
            {
                using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(iconPath);
                var memoryGroup = image.GetPixelMemoryGroup();
                Memory<byte> array = new byte[memoryGroup.TotalLength * sizeof(Rgba32)];
                var block = MemoryMarshal.Cast<byte, Rgba32>(array.Span);
                foreach (var memory in memoryGroup)
                {
                    memory.Span.CopyTo(block);
                    block = block.Slice(memory.Length);
                }

                var icon = new RawImage(image.Width, image.Height, array);
                window.SetWindowIcon(ref icon);
            }
        }
    }
}
