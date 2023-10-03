using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanBufferSetup
    { 
        private readonly VulkanImageSetup _imageSetup;
        private readonly ILogger _logger;

        public VulkanBufferSetup(VulkanImageSetup imageSetup, ILogger logger)
        {
            _imageSetup = imageSetup;
            _logger = logger;
        }

        public VulkanBuffer BufferCreate(VulkanContext context, ulong size, BufferUsageFlags usage, MemoryPropertyFlags memoryPropertyFlags, bool bindOnCreate)
        {
            BufferCreateInfo bufferInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = size,
                Usage = usage,
                SharingMode = SharingMode.Exclusive, //NOTE: only used in one queue
            };
            Buffer buffer = default;
            
            if (context.Vk.CreateBuffer(context.Device.LogicalDevice, bufferInfo, context.Allocator, &buffer) != Result.Success)
            {
                throw new Exception("Failed to create buffer!");
            }

            context.Vk.GetBufferMemoryRequirements(context.Device.LogicalDevice, buffer, out MemoryRequirements memRequirements);
            var memoryIndex = _imageSetup.FindMemoryIndex(context, memRequirements.MemoryTypeBits, memoryPropertyFlags);

            MemoryAllocateInfo allocateInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = memoryIndex,
            };
            DeviceMemory memory = default;
            if (context.Vk.AllocateMemory(context.Device.LogicalDevice, allocateInfo, null, &memory) != Result.Success)
            {
                throw new Exception("Failed to allocate buffer memory!");
            }

            var vulkanBuffer = new VulkanBuffer()
            {
                Handle = buffer,
                Memory = memory,
                TotalSize = size,
                Usage = usage,
                MemoryPropertyFlags = memoryPropertyFlags,
                MemoryIndex = memoryIndex,
            };

            if(bindOnCreate)
            {
                vulkanBuffer = BufferBind(context, vulkanBuffer, 0);
            }

            return vulkanBuffer;
        }

        public VulkanBuffer BufferDestroy(VulkanContext context, VulkanBuffer vulkanBuffer) 
        {
            throw new NotImplementedException(); 
        }

        public VulkanBuffer BufferResize(VulkanContext context, ulong newSize, VulkanBuffer vulkanBuffer, Queue queue, CommandPool pool)
        {
            throw new NotImplementedException();
        }

        public VulkanBuffer BufferBind(VulkanContext context, VulkanBuffer vulkanBuffer, ulong offset)
        {
            throw new NotImplementedException();
        }

        public VulkanBuffer BufferLockMemory(VulkanContext context, VulkanBuffer vulkanBuffer, ulong offset, ulong size, uint flags)
        {
            throw new NotImplementedException();
        }

        public VulkanBuffer BufferUnlockMemory(VulkanContext context, VulkanBuffer vulkanBuffer)
        {
            throw new NotImplementedException();
        }

        public VulkanBuffer BufferLoadData(VulkanContext context, VulkanBuffer vulkanBuffer, ulong offset, ulong size, uint flags, Span<byte> data)
        {
            throw new NotImplementedException();
        }

        public void BufferCopyTo(VulkanContext context, CommandPool pool, Fence fence, Queue queue, Buffer source, ulong sourceOffset, Buffer dest, ulong destOffset, ulong size)
        {
            throw new NotImplementedException();
        }
    }
}
