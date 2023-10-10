using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanCommandBufferSetup
    {
        private ILogger _logger;

        public VulkanCommandBufferSetup(ILogger logger)
        {
            _logger = logger;
        }

        public VulkanCommandBuffer CommandBufferAllocate(VulkanContext context, CommandPool commandPool, bool isPrimary)
        {
            CommandBufferAllocateInfo allocateInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = commandPool,
                Level = isPrimary ? CommandBufferLevel.Primary : CommandBufferLevel.Secondary,
                CommandBufferCount = 1,
            };

            context.Vk.AllocateCommandBuffers(context.Device.LogicalDevice, allocateInfo, out CommandBuffer commandBuffer);

            var vulkanCommandBuffer = new VulkanCommandBuffer()
            {
                State = CommandBufferState.NotAllocated,
                Handle = commandBuffer,
            };

            vulkanCommandBuffer.State = CommandBufferState.Ready;

            return vulkanCommandBuffer;
        }

        public VulkanCommandBuffer CommandBufferFree(VulkanContext context, CommandPool commandPool, VulkanCommandBuffer vulkanCommandBuffer)
        {
            context.Vk.FreeCommandBuffers(context.Device.LogicalDevice, commandPool, 1, vulkanCommandBuffer.Handle);

            vulkanCommandBuffer.Handle = default;
            vulkanCommandBuffer.State = CommandBufferState.NotAllocated;
            return vulkanCommandBuffer;
        }

        public VulkanCommandBuffer CommandBufferBegin(VulkanContext context, VulkanCommandBuffer vulkanCommandBuffer, bool isSingleUse, bool isRenderpassContinue, bool isSimultaneousUse)
        {
            var flags = CommandBufferUsageFlags.None;
            if (isSingleUse)
            {
                flags |= CommandBufferUsageFlags.OneTimeSubmitBit;
            }
            if (isRenderpassContinue)
            {
                flags |= CommandBufferUsageFlags.RenderPassContinueBit;
            }
            if (isSimultaneousUse)
            {
                flags |= CommandBufferUsageFlags.SimultaneousUseBit;
            }

            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = flags,
            };
            context.Vk.BeginCommandBuffer(vulkanCommandBuffer.Handle, beginInfo);

            vulkanCommandBuffer.State = CommandBufferState.Recording;
            return vulkanCommandBuffer;
        }

        public VulkanCommandBuffer CommandBufferEnd(VulkanContext context, VulkanCommandBuffer vulkanCommandBuffer)
        {
            context.Vk.EndCommandBuffer(vulkanCommandBuffer.Handle);
            vulkanCommandBuffer.State = CommandBufferState.RecordingEnded; //TODO: Check states later

            return vulkanCommandBuffer;

        }

        public VulkanCommandBuffer CommandBufferUpdateSubmitted(VulkanContext context, VulkanCommandBuffer vulkanCommandBuffer)
        {
            vulkanCommandBuffer.State = CommandBufferState.Submitted;
            return vulkanCommandBuffer;
        }

        public VulkanCommandBuffer CommandBufferReset(VulkanContext context, VulkanCommandBuffer vulkanCommandBuffer)
        {
            vulkanCommandBuffer.State = CommandBufferState.Ready;
            return vulkanCommandBuffer;
        }

        /// <summary>
        /// Allocates and begins reocrding to the command buffer.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="commandPool"></param>
        /// <returns></returns>
        public VulkanCommandBuffer CommandBufferAllocateAndBeginSingleUse(VulkanContext context, CommandPool commandPool)
        {
            var vulkanCommandBuffer = CommandBufferAllocate(context, commandPool, true);
            vulkanCommandBuffer = CommandBufferBegin(context, vulkanCommandBuffer, true, false, false);
            return vulkanCommandBuffer;
        }

        /// <summary>
        /// Ends recording, submits to and waits for queue operation and frees the provided command buffer.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="commandPool"></param>
        /// <returns></returns>
        public VulkanCommandBuffer CommandBufferEndSingleUse(VulkanContext context, CommandPool commandPool, VulkanCommandBuffer vulkanCommandBuffer, Queue queue)
        {
            //end the buffer
            vulkanCommandBuffer = CommandBufferEnd(context, vulkanCommandBuffer);

            // Submit the queue
            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &vulkanCommandBuffer.Handle,
            };

            context.Vk.QueueSubmit(queue, 1, submitInfo, default);
            //wait for it to finish
            context.Vk.QueueWaitIdle(queue);

            CommandBufferFree(context, commandPool, vulkanCommandBuffer);
            return vulkanCommandBuffer;
        }
    }
}
