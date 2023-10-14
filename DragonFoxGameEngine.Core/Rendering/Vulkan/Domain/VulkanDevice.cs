using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct VulkanDevice
    {
        public PhysicalDevice PhysicalDevice;
        public Device LogicalDevice;
        public PhysicalDeviceQueueFamilyInfo QueueFamilyIndices;
        public VulkanSwapchainSupportInfo SwapchainSupport;

        public Queue GraphicsQueue;
        public Queue PresentQueue;
        public Queue TransferQueue;
        public Queue? ComputeQueue;
        public bool SupportsDeviceLocalHostVisible { get; init; }

        public CommandPool GraphicsCommandPool;

        public PhysicalDeviceProperties Properties;
        public PhysicalDeviceFeatures Features;
        public PhysicalDeviceMemoryProperties Memory;

        public Format DepthFormat;

    }
}
