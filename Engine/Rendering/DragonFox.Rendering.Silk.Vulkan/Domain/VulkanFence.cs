using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public readonly struct VulkanFence
    {
        public Fence Handle { get; }


        public VulkanFence(Fence fence)
        {
            Handle = fence;
        }
    }
}
