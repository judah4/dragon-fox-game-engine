﻿
using Silk.NET.Vulkan;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Domain
{
    public enum CommandBufferState
    {
        Ready,
        Recording,
        InRenderPass,
        RecordingEnded,
        Submitted,
        NotAllocated,
    }

    public struct VulkanCommandBuffer
    {
        public CommandBuffer Handle;

        //Command buffer state.
        public CommandBufferState State;
    }
}
