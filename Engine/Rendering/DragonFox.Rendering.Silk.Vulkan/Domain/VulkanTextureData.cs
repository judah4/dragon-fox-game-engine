using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public readonly struct VulkanTextureData
    {
        public VulkanImage Image { get; init; }
        public Sampler Sampler { get; init; }

        public VulkanTextureData(VulkanImage image, Sampler sampler)
        {
            Image = image;
            Sampler = sampler;
        }
    }
}
