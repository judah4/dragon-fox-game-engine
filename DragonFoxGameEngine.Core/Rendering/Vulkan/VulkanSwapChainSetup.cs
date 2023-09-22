using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.OpenAL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public class VulkanSwapChainSetup
    {

        private readonly ILogger _logger;

        public VulkanSwapChainSetup(ILogger logger)
        {
            _logger = logger;
        }

        public VulkanSwapchain Create(VulkanContext context, Vector2D<int> size)
        {
            return InnerCreate(context, size);
        }

        public VulkanSwapchain Recreate(VulkanContext context, Vector2D<int> size, VulkanSwapchain swapchain)
        {
            InnerDestroy(context, swapchain);
            return InnerCreate(context, size);
        }

        public void Destroy(VulkanContext context, VulkanSwapchain swapchain)
        {
            InnerDestroy(context, swapchain);
        }

        public uint AquireNextImageIndex(VulkanContext context, VulkanSwapchain swapchain, ulong timeoutNs, Silk.NET.Vulkan.Semaphore semaphore, Fence fence)
        {
            uint imageIndex = 0;
            var result = swapchain.KhrSwapchain.AcquireNextImage(context.Device.LogicalDevice, swapchain.Swapchain, timeoutNs, semaphore, fence, ref imageIndex);

            if (result == Result.ErrorOutOfDateKhr)
            {
                swapchain = Recreate(context, context.FramebufferSize, swapchain);
                return imageIndex;
            }
            else if (result != Result.Success && result != Result.SuboptimalKhr)
            {
                throw new Exception("failed to acquire swap chain image!");
            }

            return imageIndex;
        }

        public uint Present(VulkanContext context, VulkanSwapchain swapchain, Queue graphicsQueue, Queue presentQueue, Silk.NET.Vulkan.Semaphore renderCompleteSemaphore, Fence fence)
        {
            throw new NotImplementedException();
        }

        private VulkanSwapchain InnerCreate(VulkanContext context, Vector2D<int> size)
        {
            throw new NotImplementedException();
        }

        private void InnerDestroy(VulkanContext context, VulkanSwapchain swapchain)
        {
            throw new NotImplementedException();
        }

    }
}
