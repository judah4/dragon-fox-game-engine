using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
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
        public CommandBuffer Handle { get; init; }

        //Command buffer state.
        public CommandBufferState State { get; set; }
    }
}
