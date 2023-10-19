using DragonGameEngine.Core.Resources;
using Silk.NET.Vulkan;
using System;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    public sealed class VulkanMaterialShader
    {
        public const int MATERIAL_SHADER_STAGE_COUNT = 2;
        /// <summary>
        /// The descriptor count per material instance
        /// </summary>
        public const int DESCRIPTOR_COUNT = 2;
        public const int SAMPLER_COUNT = 1;

        //TODO: make configurable
        /// <summary>
        /// Max number of material instances
        /// </summary>
        public const int MAX_MATERIAL_COUNT = 1024;

        /// <summary>
        /// Vertex, Fragment
        /// </summary>
        public VulkanShaderStage[] ShaderStages;

        public DescriptorPool GlobalDescriptorPool;
        /// <summary>
        /// Global descriptor set layout
        /// </summary>
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

        public DescriptorPool ObjectDescriptorPool;
        /// <summary>
        /// Local descriptor set layout
        /// </summary>
        public DescriptorSetLayout ObjectDescriptorSetLayout;
        public VulkanBuffer ObjectUniformBuffer;

        // TODO: manage a free list of some kind here instead
        public uint ObjectUniformBufferIndex { get; set; }

        public TextureUse[] SamplerUses { get; } = new TextureUse[SAMPLER_COUNT];

        //TODO: make dynamic
        public VulkanMaterialShaderInstanceState[] InstanceStates { get; init; }

        public VulkanMaterialShader()
        {
            ShaderStages = new VulkanShaderStage[MATERIAL_SHADER_STAGE_COUNT];
            GlobalDescriptorSets = new DescriptorSet[3];
            InstanceStates = new VulkanMaterialShaderInstanceState[MAX_MATERIAL_COUNT];
            for(int cnt = 0; cnt < InstanceStates.Length; cnt++)
            {
                InstanceStates[cnt] = new VulkanMaterialShaderInstanceState()
                {
                    DescriptorSets = new DescriptorSet[3],
                    DescriptorStates = new VulkanDescriptorState[DESCRIPTOR_COUNT],
                };
            }
        }
    }
}
