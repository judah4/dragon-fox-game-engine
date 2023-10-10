using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct VulkanFence
    {
        public Fence Handle;
        public bool IsSignaled;
    }
}
