using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public enum RenderpassState
    {
        Ready,
        Recording,
        InRenderPass,
        RecordingEnded,
        Submitted,
        NotAllocated,
    }

    public struct VulkanRenderpass
    {
        public RenderPass Handle { get; init; }
        public Rect2D Rect { get; set; }
        public System.Drawing.Color Color { get; init; }
        public float Depth { get; init; }
        public uint Stencil { get; init; }

        public RenderpassState State { get; set; }
    }
}
