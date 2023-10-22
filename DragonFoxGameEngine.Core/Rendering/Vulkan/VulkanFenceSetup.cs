using DragonGameEngine.Core.Exceptions.Vulkan;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using Foxis.Library;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonGameEngine.Core.Rendering.Vulkan
{
    public unsafe sealed class VulkanFenceSetup
    {
        private readonly ILogger _logger;
        public VulkanFenceSetup(ILogger logger)
        {
            _logger = logger;
        }

        public VulkanFence FenceCreate(VulkanContext context, bool createSignaled)
        {

            FenceCreateInfo fenceInfo = new()
            {
                SType = StructureType.FenceCreateInfo,
            };
            if (createSignaled)
            {
                fenceInfo.Flags = FenceCreateFlags.SignaledBit;
            }

            var fenceResult = context.Vk.CreateFence(context.Device.LogicalDevice, fenceInfo, context.Allocator, out var fence);
            if (fenceResult != Silk.NET.Vulkan.Result.Success)
            {
                throw new VulkanResultException(fenceResult, "Failed to create fence!");
            }

            var vulkanFence = new VulkanFence(fence);

            _logger.LogDebug("Fence created");
            return vulkanFence;
        }

        public VulkanFence FenceDestroy(VulkanContext context, VulkanFence vulkanFence)
        {
            if (vulkanFence.Handle.Handle != 0)
            {
                context.Vk.DestroyFence(context.Device.LogicalDevice, vulkanFence.Handle, context.Allocator);
            }

            vulkanFence = default;
            _logger.LogDebug("Fence destroyed");
            return vulkanFence;
        }

        public Result<VulkanFence> FenceWait(VulkanContext context, VulkanFence vulkanFence, ulong timeoutNs)
        {
            var result = context.Vk.WaitForFences(context.Device.LogicalDevice, 1, vulkanFence.Handle, true, timeoutNs);

            if(VulkanUtils.ResultIsSuccess(result))
            {
                return Foxis.Library.Result.Ok(vulkanFence);
            }
            return Foxis.Library.Result.Fail<VulkanFence>(VulkanUtils.FormattedResult(result));
        }

        public VulkanFence FenceReset(VulkanContext context, VulkanFence vulkanFence)
        {
            context.Vk.ResetFences(context.Device.LogicalDevice, 1, vulkanFence.Handle);
            return vulkanFence;
        }
    }
}
