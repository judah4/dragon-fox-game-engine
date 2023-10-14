using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    public struct VulkanMaterialShaderObjectState
    {
        /// <summary>
        /// The descriptor count per object
        /// </summary>
        public const int DESCRIPTOR_COUNT = 2;

        //per frame
        public DescriptorSet[] DescriptorSets { get; init; }

        public VulkanDescriptorState[] DescriptorStates { get; init; }
    }
}
