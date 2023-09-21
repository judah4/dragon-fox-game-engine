using Microsoft.Extensions.Logging;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using System.Runtime.InteropServices;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public class VulkanBackendRenderer : IRenderer
    {
        private readonly ILogger _logger;

        private VulkanContext? _context;

        public VulkanBackendRenderer(ILogger logger)
        {
            _logger = logger;
            _logger.LogInformation($"Vulkan initialized.");
        }

        public unsafe VulkanContext Init(string applicationName, IWindow window)
        {
            var vk = Vk.GetApi();

            //if (EnableValidationLayers && !CheckValidationLayerSupport())
            //{
            //    throw new Exception("validation layers requested, but not available!");
            //}

            var gameVersion = ApplicationInfo.GameVersion;
            var engineVersion = ApplicationInfo.EngineVersion;
            Silk.NET.Vulkan.ApplicationInfo appInfo = new()
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(applicationName),
                ApplicationVersion = new Version32((uint)gameVersion.Major, (uint)gameVersion.Minor, (uint)gameVersion.Revision),
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi(ApplicationInfo.GAME_ENGINE_NAME),
                EngineVersion = new Version32((uint)engineVersion.Major, (uint)engineVersion.Minor, (uint)engineVersion.Revision),
                ApiVersion = Vk.Version12,
            };

            InstanceCreateInfo createInfo = new()
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo
            };

            //var extensions = GetRequiredExtensions();
            //createInfo.EnabledExtensionCount = (uint)extensions.Length;
            //createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);

            //if (EnableValidationLayers)
            //{
            //    createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            //    createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);

            //    DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            //    PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            //    createInfo.PNext = &debugCreateInfo;
            //}
            //else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PNext = null;
            }

            AllocationCallbacks* allocator = null; //null for now. Might deal with it later
            if (vk.CreateInstance(createInfo, allocator, out var instance) != Result.Success)
            {
                throw new Exception("failed to create Vulkan instance!");
            }

            Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
            Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

            //if (EnableValidationLayers)
            //{
            //    SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
            //}

            return new VulkanContext(vk, window, instance, allocator);
        }

        public unsafe void Shutdown()
        {
            _context.Vk.DestroyInstance(_context.Instance, _context.Allocator);
            _context.Vk.Dispose();
        }

        public void Resized(Vector2D<int> size)
        {

        }

        public void BeginFrame(double deltaTime)
        {

        }

        public void EndFrame(double deltaTime)
        {

        }
    }
}
