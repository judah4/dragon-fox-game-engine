using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public readonly struct VulkanTextureData
    {
        public readonly VulkanImage Image;
        public readonly Sampler Sampler;

        public VulkanTextureData(VulkanImage image, Sampler sampler)
        {
            Image = image;
            Sampler = sampler;
        }
    }
}
