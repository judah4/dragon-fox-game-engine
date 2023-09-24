using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using Silk.NET.Vulkan;
using System;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanCommandBufferSetup
    {
        public VulkanCommandBuffer CommandBufferAllocate(VulkanContext context, CommandPool commandPool, bool isPrimary)
        {
            throw new NotImplementedException();
        }

        public VulkanCommandBuffer CommandBufferFree(VulkanContext context, CommandPool commandPool, VulkanCommandBuffer vulkanCommandBuffer)
        {
            throw new NotImplementedException();
        }

        public VulkanCommandBuffer CommandBufferBegin(VulkanContext context, VulkanCommandBuffer vulkanCommandBuffer, bool isSingleUse, bool isRenderpassContinue, bool isSimultaneousUse)
        {
            throw new NotImplementedException();
        }

        public VulkanCommandBuffer CommandBufferEnd(VulkanContext context, VulkanCommandBuffer vulkanCommandBuffer)
        {
            throw new NotImplementedException();
        }

        public VulkanCommandBuffer CommandBufferUpdateSubmitted(VulkanContext context, VulkanCommandBuffer vulkanCommandBuffer)
        {
            throw new NotImplementedException();
        }

        public VulkanCommandBuffer CommandBufferReset(VulkanContext context, VulkanCommandBuffer vulkanCommandBuffer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Allocates and begins reocrding to the command buffer.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="commandPool"></param>
        /// <returns></returns>
        public VulkanCommandBuffer CommandBufferAllocateAndBeginSingleUse(VulkanContext context, CommandPool commandPool)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ends recording, submits to and waits for queue operation and frees the provided command buffer.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="commandPool"></param>
        /// <returns></returns>
        public VulkanCommandBuffer CommandBufferEndSingleUse(VulkanContext context, CommandPool commandPool, VulkanCommandBuffer vulkanCommandBuffer, Queue queue)
        {
            throw new NotImplementedException();
        }
    }
}
