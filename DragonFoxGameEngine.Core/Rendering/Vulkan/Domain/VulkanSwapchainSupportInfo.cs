using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public readonly record struct VulkanSwapchainSupportInfo
    {
        public readonly SurfaceCapabilitiesKHR Capabilities { get; init; }
        public readonly SurfaceFormatKHR[] Formats { get; init; }
        public readonly PresentModeKHR[] PresentModes { get; init; }
    }
}
