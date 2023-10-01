using Silk.NET.Vulkan;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    public struct VulkanShaderStage
    {
        public ShaderModuleCreateInfo CreateInfo;
        public ShaderModule Handle;
        public PipelineShaderStageCreateInfo ShaderStageCreateInfo;
    }
}
