using Silk.NET.Vulkan;
using System;

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

    [Flags]
    public enum RenderpassClearFlags
    {
        None = 0x0,
        ClearColorBufferFlag = 0x1,
        ClearDepthBufferFlag = 0x2,
        ClearStencilBufferFlag = 0x4,
    }

    public struct VulkanRenderpass
    {
        public RenderPass Handle { get; init; }
        public Rect2D Rect { get; set; }
        public System.Drawing.Color Color { get; init; }
        public float Depth { get; init; }
        public uint Stencil { get; init; }

        public RenderpassClearFlags ClearFlags { get; init; }
        public bool HasPreviousPass { get; init; }
        public bool HasNextPass {  get; init; }

        public RenderpassState State { get; set; }
    }
}
