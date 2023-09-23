using Silk.NET.Vulkan;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public struct VulkanSwapchainSupportInfo
    {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats;
        public PresentModeKHR[] PresentModes;
    }
}
