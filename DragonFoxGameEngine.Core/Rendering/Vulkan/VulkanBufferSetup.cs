using DragonGameEngine.Core.Exceptions.Vulkan;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace DragonGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanBufferSetup
    {
        private readonly VulkanImageSetup _imageSetup;
        private readonly VulkanCommandBufferSetup _commandBufferSetup;
        private readonly ILogger _logger;

        public VulkanBufferSetup(VulkanImageSetup imageSetup, VulkanCommandBufferSetup commandBufferSetup, ILogger logger)
        {
            _imageSetup = imageSetup;
            _commandBufferSetup = commandBufferSetup;
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

            var createBufferResult = context.Vk.CreateBuffer(context.Device.LogicalDevice, bufferInfo, context.Allocator, &buffer);
            if (createBufferResult != Result.Success)
            {
                throw new VulkanResultException(createBufferResult, "Failed to create buffer!");
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
            var allocMemoryResult = context.Vk.AllocateMemory(context.Device.LogicalDevice, allocateInfo, context.Allocator, &memory);
            if (allocMemoryResult != Result.Success)
            {
                throw new VulkanResultException(allocMemoryResult, "Failed to allocate buffer memory!");
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

            if (bindOnCreate)
            {
                BufferBind(context, vulkanBuffer, 0);
            }

            return vulkanBuffer;
        }

        public VulkanBuffer BufferDestroy(VulkanContext context, VulkanBuffer vulkanBuffer)
        {
            if (vulkanBuffer.Memory.Handle != default)
            {
                context.Vk.FreeMemory(context.Device.LogicalDevice, vulkanBuffer.Memory, context.Allocator);
            }

            if (vulkanBuffer.Handle.Handle != default)
            {
                context.Vk.DestroyBuffer(context.Device.LogicalDevice, vulkanBuffer.Handle, context.Allocator);
            }

            vulkanBuffer = default;

            return vulkanBuffer;
        }

        public VulkanBuffer BufferResize(VulkanContext context, ulong newSize, VulkanBuffer vulkanBuffer, Queue queue, CommandPool pool)
        {
            BufferCreateInfo bufferInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = newSize,
                Usage = vulkanBuffer.Usage,
                SharingMode = SharingMode.Exclusive, //NOTE: only used in one queue
            };
            Buffer buffer = default;

            var createResult = context.Vk.CreateBuffer(context.Device.LogicalDevice, bufferInfo, context.Allocator, &buffer);
            if (createResult != Result.Success)
            {
                throw new VulkanResultException(createResult, "Failed to create buffer!");
            }

            context.Vk.GetBufferMemoryRequirements(context.Device.LogicalDevice, buffer, out MemoryRequirements memRequirements);
            var memoryIndex = vulkanBuffer.MemoryIndex;

            MemoryAllocateInfo allocateInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = memoryIndex,
            };
            DeviceMemory memory = default;
            var allocateMemResult = context.Vk.AllocateMemory(context.Device.LogicalDevice, allocateInfo, null, &memory);
            if (allocateMemResult != Result.Success)
            {
                throw new VulkanResultException(allocateMemResult, "Failed to allocate buffer memory!");
            }

            var bindResult = context.Vk.BindBufferMemory(context.Device.LogicalDevice, buffer, memory, 0);
            if (bindResult != Result.Success)
            {
                throw new VulkanResultException(bindResult, "Bind buffer failed!");
            }

            BufferCopyTo(context, pool, default, queue, vulkanBuffer.Handle, 0, buffer, 0, vulkanBuffer.TotalSize);

            //make sure everything is finished
            context.Vk.DeviceWaitIdle(context.Device.LogicalDevice);

            if (vulkanBuffer.Memory.Handle != default)
            {
                context.Vk.FreeMemory(context.Device.LogicalDevice, vulkanBuffer.Memory, context.Allocator);
                vulkanBuffer.Memory = default;
            }

            if (vulkanBuffer.Handle.Handle != default)
            {
                context.Vk.DestroyBuffer(context.Device.LogicalDevice, vulkanBuffer.Handle, context.Allocator);
                vulkanBuffer.Handle = default;
            }

            //set new properties
            vulkanBuffer.TotalSize = newSize;
            vulkanBuffer.Handle = buffer;
            vulkanBuffer.Memory = memory;

            return vulkanBuffer;
        }

        public void BufferBind(VulkanContext context, VulkanBuffer vulkanBuffer, ulong offset)
        {
            var bindResult = context.Vk.BindBufferMemory(context.Device.LogicalDevice, vulkanBuffer.Handle, vulkanBuffer.Memory, offset);
            if (bindResult != Result.Success)
            {
                throw new VulkanResultException(bindResult, "Bind buffer failed!");
            }
        }

        public Span<T> BufferLockMemory<T>(VulkanContext context, VulkanBuffer vulkanBuffer, ulong offset, ulong size, uint flags)
        {
            void* data;
            var mapMemoryResult = context.Vk.MapMemory(context.Device.LogicalDevice, vulkanBuffer.Memory, offset, size, flags, &data);
            if (mapMemoryResult != Result.Success)
            {
                throw new VulkanResultException(mapMemoryResult, "Lock buffer failed!");

            }
            //img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize)); //from demo
            return new Span<T>(data, (int)size);
        }

        public void BufferUnlockMemory(VulkanContext context, VulkanBuffer vulkanBuffer)
        {
            context.Vk.UnmapMemory(context.Device.LogicalDevice, vulkanBuffer.Memory);
        }

        public void BufferLoadData<T>(VulkanContext context, VulkanBuffer vulkanBuffer, ulong offset, ulong size, uint flags, Span<T> data)
        {
            var bufferSpan = BufferLockMemory<T>(context, vulkanBuffer, offset, size, flags);

            //copy
            data.CopyTo(bufferSpan);

            BufferUnlockMemory(context, vulkanBuffer);
        }

        public void BufferCopyTo(VulkanContext context, CommandPool pool, Fence fence, Queue queue, Buffer source, ulong sourceOffset, Buffer dest, ulong destOffset, ulong size)
        {
            context.Vk.QueueWaitIdle(queue);
            //create a one time use command buffer
            var commandBuffer = _commandBufferSetup.CommandBufferAllocateAndBeginSingleUse(context, pool);

            var copyRegion = new BufferCopy(sourceOffset, destOffset, size);

            context.Vk.CmdCopyBuffer(commandBuffer.Handle, source, dest, 1, copyRegion);

            //submit the vuffer for execution and wait for it to complete
            _commandBufferSetup.CommandBufferEndSingleUse(context, pool, commandBuffer, queue);
        }
    }
}
