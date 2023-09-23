using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System;
using System.Linq;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanPlatform
    {
        public static string[] GetRequiredExtensions(IWindow window)
        {
            var glfwExtensions = window.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
            var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

#if DEBUG
            extensions = extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
#endif

            return extensions;
        }

        public static void CreateSurface(VulkanContext context, ILogger logger)
        {
            if (!context.Vk.TryGetInstanceExtension<KhrSurface>(context.Instance, out var khrSurface))
            {
                throw new NotSupportedException("KHR_surface extension not found.");
            }

            var surface = context.Window.VkSurface!.Create<AllocationCallbacks>(context.Instance.ToHandle(), context.Allocator).ToSurface();

            context.SetupSurface(khrSurface, surface);
            logger.LogDebug("Surface setup");

        }
    }
}
