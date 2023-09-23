using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Windowing;
using System;
using System.Runtime.InteropServices;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public sealed unsafe class VulkanBackendRenderer : IRenderer
    {
        private readonly ILogger _logger;

        private VulkanContext? _context;
        private VulkanDeviceSetup _deviceSetup;
        private VulkanSwapchainSetup _swapchainSetup;
        private VulkanImageSetup _imageSetup;


#if DEBUG
        private readonly bool EnableValidationLayers = true; //enable when tools are installed. Add to config
#else
        private readonly bool EnableValidationLayers = false;
#endif

        private readonly string[] _validationLayers = new[]
{
            "VK_LAYER_KHRONOS_validation"
        };

        public VulkanBackendRenderer(ILogger logger)
        {
            _logger = logger;
            _deviceSetup = new VulkanDeviceSetup(logger);
            _imageSetup = new VulkanImageSetup(logger);
            _swapchainSetup = new VulkanSwapchainSetup(logger, _deviceSetup, _imageSetup);
        }

        public VulkanContext Init(string applicationName, IWindow window)
        {
            if(_context != null)
            {
                throw new Exception("Vulkan is already initialized!");
            }

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

            var extensions = VulkanPlatform.GetRequiredExtensions(window);

#if DEBUG
            _logger.LogDebug($"Required Extensions: {string.Join(",", extensions)}");
#endif

            createInfo.EnabledExtensionCount = (uint)extensions.Length;
            createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);

            if (EnableValidationLayers)
            {
                uint availableLayerCount = 0;
                vk.EnumerateInstanceLayerProperties(ref availableLayerCount, null); //get count
                var availableLayers = new LayerProperties[availableLayerCount];
                fixed (LayerProperties* availableLayersPtr = availableLayers)
                {
                    if (vk.EnumerateInstanceLayerProperties(ref availableLayerCount, availableLayersPtr) == Result.Success)
                    {
                        foreach (var layer in _validationLayers)
                        {
                            bool found = false;

                            for (int cnt = 0; cnt < availableLayerCount; cnt++)
                            {
                                var availableLayer = availableLayersPtr[cnt];

                                var availableName = Marshal.PtrToStringAnsi((nint)availableLayer.LayerName);
                                if (availableName == layer)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                throw new Exception($"Required Validation Layer is missing: {layer}");
                            }
                            _logger.LogDebug($"Validation Layer {layer} added.");
                        }
                    }
                }

                createInfo.EnabledLayerCount = (uint)_validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(_validationLayers);

                DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
                PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
                createInfo.PNext = &debugCreateInfo;

            }
            else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PNext = null;
            }

            AllocationCallbacks* allocator = null; //null for now. Might deal with it later
            if (vk.CreateInstance(createInfo, allocator, out var instance) != Result.Success)
            {
                throw new Exception("failed to create Vulkan instance!");
            }
            ExtDebugUtils? debugUtils = null;
            DebugUtilsMessengerEXT debugMessenger = default;
            if (EnableValidationLayers)
            {
                var debuggingUtils = SetupDebugMessenger(vk, instance);
                if(debuggingUtils.HasValue)
                {
                    debugUtils = debuggingUtils.Value.Item1;
                    debugMessenger = debuggingUtils.Value.Item2;
                }
            }

            Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
            Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

            if (EnableValidationLayers)
            {
                SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
            }

            var framebufferSize = new Vector2D<uint>((uint)window.Size.X, (uint)window.Size.Y);
            _context = new VulkanContext(vk, window, instance, allocator, debugUtils, debugMessenger, framebufferSize);

            VulkanPlatform.CreateSurface(_context, _logger);

            //Device creation
            _deviceSetup.Create(_context);

            //Swapchain
            _swapchainSetup.Create(_context, _context.FramebufferSize);

            _logger.LogInformation($"Vulkan initialized.");
            return _context;
        }

        public void Shutdown()
        {
            if(_context == null)
                return;

            _swapchainSetup.Destroy(_context, _context.Swapchain);

            _deviceSetup.Destroy(_context);

            if (EnableValidationLayers)
            {
                //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
                _context!.DebugUtils?.DestroyDebugUtilsMessenger(_context.Instance, _context.DebugMessenger, null);
            }

            if(_context.Surface.HasValue)
            {
                _context.KhrSurface!.DestroySurface(_context.Instance, _context.Surface.Value, _context.Allocator);
            }

            _context?.Vk.DestroyInstance(_context.Instance, _context.Allocator);
            _context?.Vk.Dispose();
            _context = null;
            _logger.LogInformation("Vulkan is shutdown.");
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

        private void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
        {
            createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
            createInfo.MessageSeverity = //DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
            createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
            createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
        }

        /// <summary>
        /// Debugging messaging.
        /// </summary>
        /// <param name="vk"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <remarks>I'm not 100% what this does -Judah</remarks>
        private (ExtDebugUtils, DebugUtilsMessengerEXT)? SetupDebugMessenger(Vk vk, Instance instance)
        {
            if (!EnableValidationLayers) return null;

            ExtDebugUtils debugUtils;
            DebugUtilsMessengerEXT debugMessenger;

            //TryGetInstanceExtension equivilant to method CreateDebugUtilsMessengerEXT from original tutorial.
            if (!vk.TryGetInstanceExtension(instance, out debugUtils)) return null;

            DebugUtilsMessengerCreateInfoEXT createInfo = new();
            PopulateDebugMessengerCreateInfo(ref createInfo);

            if (debugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out debugMessenger) != Result.Success)
            {
                throw new Exception("failed to set up debug messenger!");
            }
            _logger.LogDebug($"Debug messenger setup.");
            return (debugUtils, debugMessenger);
        }

        /// <summary>
        /// Send debugging info to console
        /// </summary>
        /// <param name="messageSeverity"></param>
        /// <param name="messageTypes"></param>
        /// <param name="pCallbackData"></param>
        /// <param name="pUserData"></param>
        /// <returns></returns>
        private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
        {
            _logger.LogDebug($"validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

            return Vk.False;
        }

    }
}
