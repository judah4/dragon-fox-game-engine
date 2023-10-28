using Foxis.Library.Freelists;
using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct VulkanBuffer
    {
        public ulong TotalSize { get; set; }
        public Buffer Handle { get; set; }
        public BufferUsageFlags Usage { get; set; }
        public bool IsLocked { get; set; }
        public DeviceMemory Memory { get; set; }
        public uint MemoryIndex { get; set; }
        public MemoryPropertyFlags MemoryPropertyFlags { get; set; }

        public FreeList Freelist { get; init; }
    }
}
