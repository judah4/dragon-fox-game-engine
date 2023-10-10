using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    public struct VulkanObjectShader
    {
        public const int OBJECT_SHADER_STAGE_COUNT = 2;

        /// <summary>
        /// Vertex, Fragment
        /// </summary>
        public VulkanShaderStage[] ShaderStages;

        public DescriptorPool GlobalDescriptorPool;
        public DescriptorSetLayout GlobalDescriptorSetLayout;

        /// <summary>
        /// Global descriptor sets per frame
        /// </summary>
        /// <remarks>
        /// One descriptor set per frame - max 3 for triple buffering.
        /// </remarks>
        public DescriptorSet[] GlobalDescriptorSets;

        /// <summary>
        /// Global uniform object
        /// </summary>
        public GlobalUniformObject GlobalUbo;

        public VulkanBuffer GlobalUniformBuffer;

        public VulkanPipeline Pipeline;

        public VulkanObjectShader()
        {
            ShaderStages = new VulkanShaderStage[OBJECT_SHADER_STAGE_COUNT];
            GlobalDescriptorSets = new DescriptorSet[3];
        }
    }
}
