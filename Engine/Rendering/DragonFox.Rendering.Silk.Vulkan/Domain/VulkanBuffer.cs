using System.ComponentModel.DataAnnotations;
using DragonFox.Utilities;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public class VulkanBuffer
    {
        public ulong TotalSize { get; set; }
        public Buffer Handle { get; set; }
        public BufferUsageFlags Usage { get; init; }
        public bool IsLocked { get; set; }
        public DeviceMemory Memory { get; set; }
        public uint MemoryIndex { get; init; }
        public MemoryPropertyFlags MemoryPropertyFlags { get; set; }

        [Required]
        public FreeList Freelist { get; init; }
    }
}
