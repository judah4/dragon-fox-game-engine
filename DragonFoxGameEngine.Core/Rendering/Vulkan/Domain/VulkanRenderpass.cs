using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Domain
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
        public RenderPass Handle;
        public Rect2D Rect;
        public System.Drawing.Color Color;
        public float Depth;
        public uint Stencil;

        public RenderpassState State;
    }
}
