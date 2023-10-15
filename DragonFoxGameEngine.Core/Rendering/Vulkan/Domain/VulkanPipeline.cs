using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public readonly struct VulkanPipeline
    {
        public Pipeline Handle { get; init; }
        public PipelineLayout PipelineLayout { get; init; }
    }
}
