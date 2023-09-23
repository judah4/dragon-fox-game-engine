using Silk.NET.Vulkan;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public struct VulkanDevice
    {
        public PhysicalDevice PhysicalDevice;
        public Device LogicalDevice;
        public PhysicalDeviceQueueFamilyInfo QueueFamilyIndices;
        public VulkanSwapchainSupportInfo SwapchainSupport;

        public PhysicalDeviceProperties Properties;
        public PhysicalDeviceFeatures Features;
        public PhysicalDeviceMemoryProperties Memory;

        public Queue GraphicsQueue;
        public Queue PresentQueue;
        public Queue TransferQueue;
        public Queue? ComputeQueue;

        public Format DepthFormat;
    }
}
