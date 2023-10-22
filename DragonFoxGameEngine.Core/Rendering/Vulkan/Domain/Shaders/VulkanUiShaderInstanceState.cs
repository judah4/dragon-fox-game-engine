﻿using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    public struct VulkanUiShaderInstanceState
    {
        //per frame
        public DescriptorSet[] DescriptorSets { get; init; }

        public VulkanDescriptorState[] DescriptorStates { get; init; }
    }
}
