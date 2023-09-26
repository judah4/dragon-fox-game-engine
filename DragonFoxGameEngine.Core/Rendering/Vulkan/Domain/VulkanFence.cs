using Silk.NET.Vulkan;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct VulkanFence
    {
        public Fence Handle;
        public bool IsSignaled;
    }
}
