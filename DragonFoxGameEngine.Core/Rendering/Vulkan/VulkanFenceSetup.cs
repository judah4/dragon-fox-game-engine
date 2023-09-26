using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenAL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanFenceSetup
    {
        private readonly ILogger _logger;
        public VulkanFenceSetup(ILogger logger)
        {
            _logger = logger;
        }

        public VulkanFence FenceCreate(VulkanContext context, bool createSignaled)
        {
            var vulkanFence = new VulkanFence()
            {
                IsSignaled = createSignaled,
            };

            FenceCreateInfo fenceInfo = new()
            {
                SType = StructureType.FenceCreateInfo,
            };
            if(createSignaled)
            {
                fenceInfo.Flags = FenceCreateFlags.SignaledBit;
            }

            if(context.Vk.CreateFence(context.Device.LogicalDevice, fenceInfo, context.Allocator, out var fence) != Result.Success)
            {
                throw new Exception("Failed to create fence!");
            }
            vulkanFence.Handle = fence;

            _logger.LogDebug("Fence created");
            return vulkanFence;
        }

        public VulkanFence FenceDestroy(VulkanContext context, VulkanFence vulkanFence)
        {
            if(vulkanFence.Handle.Handle != 0)
            {
                context.Vk.DestroyFence(context.Device.LogicalDevice, vulkanFence.Handle, context.Allocator);
            }

            vulkanFence = default;
            _logger.LogDebug("Fence destroyed");
            return vulkanFence;
        }

        public EngineResult<VulkanFence> FenceWait(VulkanContext context, VulkanFence vulkanFence, ulong timeoutNs)
        {
            if(vulkanFence.IsSignaled)
                return EngineResult.Ok(vulkanFence);

            var result = context.Vk.WaitForFences(context.Device.LogicalDevice, 1, vulkanFence.Handle, true, timeoutNs);
            switch(result)
            {
                case Result.Success:
                    vulkanFence.IsSignaled = true;
                    return EngineResult.Ok(vulkanFence);
                case Result.Timeout:
                    _logger.LogWarning($"Fence Wait - {result}");
                    return EngineResult.Fail<VulkanFence>(result.ToString());
                default:
                    _logger.LogError($"Fence Wait - {result}");
                    return EngineResult.Fail<VulkanFence>(result.ToString());
            }
        }

        public VulkanFence FenceReset(VulkanContext context, VulkanFence vulkanFence)
        {
            if(vulkanFence.IsSignaled)
            {
                context.Vk.ResetFences(context.Device.LogicalDevice, 1, vulkanFence.Handle);
                vulkanFence.IsSignaled = false;
            }
            return vulkanFence;
        }
    }
}
