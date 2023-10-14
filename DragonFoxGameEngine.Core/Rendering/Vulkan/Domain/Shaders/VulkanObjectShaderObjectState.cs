using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    public struct VulkanObjectShaderObjectState
    {
        /// <summary>
        /// The descriptor count per object
        /// </summary>
        public const int DESCRIPTOR_COUNT = 2;

        //per frame
        public DescriptorSet[] DescriptorSets;

        public VulkanDescriptorState[] DescriptorStates;
    }
}
