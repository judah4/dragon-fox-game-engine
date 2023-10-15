using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public record struct VulkanSwapchainSupportInfo
    {
        public SurfaceCapabilitiesKHR Capabilities { get; init; }
        public SurfaceFormatKHR[] Formats { get; init; }
        public PresentModeKHR[] PresentModes { get; init; }
    }
}
