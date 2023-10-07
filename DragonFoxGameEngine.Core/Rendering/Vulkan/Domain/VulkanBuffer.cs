using Silk.NET.Vulkan;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct VulkanBuffer
    {
        public ulong TotalSize;
        public Buffer Handle;
        public BufferUsageFlags Usage;
        public bool IsLocked;
        public DeviceMemory Memory;
        public uint MemoryIndex;
        public MemoryPropertyFlags MemoryPropertyFlags;
    }
}
