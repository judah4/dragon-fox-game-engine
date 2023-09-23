using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct VulkanImage
    {
        public Silk.NET.Vulkan.Image Handle;
        public DeviceMemory Memory;
        public ImageView ImageView;
        public Vector2D<uint> Size;
    }
}
