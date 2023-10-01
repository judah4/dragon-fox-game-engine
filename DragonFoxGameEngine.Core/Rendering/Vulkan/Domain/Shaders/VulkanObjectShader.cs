
namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    public struct VulkanObjectShader
    {
        public const int OBJECT_SHADER_STAGE_COUNT = 2;

        /// <summary>
        /// Vertex, Fragment
        /// </summary>
        public VulkanShaderStage[] ShaderStages;
        public VulkanPipeline Pipeline;

        public VulkanObjectShader()
        {
            ShaderStages = new VulkanShaderStage[OBJECT_SHADER_STAGE_COUNT];
        }
    }
}
