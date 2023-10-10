using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct VulkanImage
    {
        public Image Handle;
        public DeviceMemory Memory;
        public ImageView ImageView;
        public Vector2D<uint> Size;
    }
}
