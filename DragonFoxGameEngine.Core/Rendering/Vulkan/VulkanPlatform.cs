using Silk.NET.Core.Native;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Windowing;

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
    }
}
