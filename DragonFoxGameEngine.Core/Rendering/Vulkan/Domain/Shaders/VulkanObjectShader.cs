using DragonGameEngine.Core.Resources;
using Silk.NET.Vulkan;
using System;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    public class VulkanObjectShader
    {
        public const int OBJECT_SHADER_STAGE_COUNT = 2;
        public const int MAX_OBJECT_COUNT = 1024;

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
        public uint ObjectUniformBufferIndex;

        //TODO: make dynamic
        public VulkanObjectShaderObjectState[] ObjectStates;

        public Texture DefaultDiffuse { get; init; }

        public VulkanObjectShader()
        {
            ShaderStages = new VulkanShaderStage[OBJECT_SHADER_STAGE_COUNT];
            GlobalDescriptorSets = new DescriptorSet[3];
            ObjectStates = new VulkanObjectShaderObjectState[MAX_OBJECT_COUNT];
            Array.Fill(ObjectStates, new VulkanObjectShaderObjectState()
            {
                DescriptorSets = new DescriptorSet[3],
                DescriptorStates = new VulkanDescriptorState[VulkanObjectShaderObjectState.DESCRIPTOR_COUNT],
            });
        }
    }
}
