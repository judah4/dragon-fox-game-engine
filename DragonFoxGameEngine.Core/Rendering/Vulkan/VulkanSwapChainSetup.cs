using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanSwapchainSetup
    {

        private readonly ILogger _logger;
        private readonly VulkanDeviceSetup _vulkanDeviceSetup;
        private readonly VulkanImageSetup _vulkanImageSetup;

        public VulkanSwapchainSetup(ILogger logger, VulkanDeviceSetup vulkanDeviceSetup, VulkanImageSetup vulkanImageSetup)
        {
            _logger = logger;
            _vulkanDeviceSetup = vulkanDeviceSetup;
            _vulkanImageSetup = vulkanImageSetup;
        }

        public VulkanSwapchain Create(VulkanContext context, Vector2D<uint> size)
        {
            return InnerCreate(context, size);
        }

        public VulkanSwapchain Recreate(VulkanContext context, Vector2D<uint> size, VulkanSwapchain swapchain)
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

        public void Present(VulkanContext context, VulkanSwapchain swapchain, Queue graphicsQueue, Queue presentQueue, Silk.NET.Vulkan.Semaphore* renderCompleteSemaphore, Fence fence, uint presentImageIndex)
        {
            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,

                WaitSemaphoreCount = 1,
                PWaitSemaphores = renderCompleteSemaphore,

                SwapchainCount = 1,
                PSwapchains = &swapchain.Swapchain,

                PImageIndices = &presentImageIndex
            };

            var result = swapchain.KhrSwapchain.QueuePresent(presentQueue, presentInfo);

            if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr)
            {
                Recreate(context, context.FramebufferSize, swapchain);
            }
            else if (result != Result.Success)
            {
                throw new Exception("failed to present swap chain image!");
            }

            //Increment and loop the index
            var currentFrame = (context.CurrentFrame + 1) % swapchain.MaxFramesInFlight;
            context.SetCurrentFrame(currentFrame);
        }

        private VulkanSwapchain InnerCreate(VulkanContext context, Vector2D<uint> size)
        {
            var swapchainExtent = new Extent2D(size.X, size.Y);
            var swapchain = new VulkanSwapchain();
            swapchain.MaxFramesInFlight = 2;

            swapchain.ImageFormat = ChooseSwapSurfaceFormat(context.Device.SwapchainSupport.Formats);
            var presentMode = ChoosePresentMode(context.Device.SwapchainSupport.PresentModes);

            var swapchainSupport = _vulkanDeviceSetup.QuerySwapChainSupport(context.Device.PhysicalDevice, context);

            if(swapchainSupport.Capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                swapchainExtent = swapchainSupport.Capabilities.CurrentExtent;
            }

            //clamp to the value allowed by the GPU
            var min = swapchainSupport.Capabilities.MinImageExtent;
            var max = swapchainSupport.Capabilities.MaxImageExtent;
            swapchainExtent.Width = Math.Clamp(swapchainExtent.Width, min.Width, max.Width);
            swapchainExtent.Height = Math.Clamp(swapchainExtent.Height, min.Height, max.Height);

            var imageCount = swapchainSupport.Capabilities.MinImageCount + 1;
            if(swapchainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapchainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapchainSupport.Capabilities.MaxImageCount;
            }

            SwapchainCreateInfoKHR creatInfo = new()
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = context.Surface!.Value,
                MinImageCount = imageCount,
                ImageFormat = swapchain.ImageFormat.Format,
                ImageColorSpace = swapchain.ImageFormat.ColorSpace,
                ImageExtent = swapchainExtent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            };

            if (context.Device.QueueFamilyIndices.GraphicsFamilyIndex != context.Device.QueueFamilyIndices.PresentFamilyIndex)
            {
                var queueFamilyIndices = stackalloc[] { context.Device.QueueFamilyIndices.GraphicsFamilyIndex, context.Device.QueueFamilyIndices.PresentFamilyIndex };
                creatInfo = creatInfo with
                {
                    ImageSharingMode = SharingMode.Concurrent,
                    QueueFamilyIndexCount = 2,
                    PQueueFamilyIndices = queueFamilyIndices,
                };
            }
            else
            {
                creatInfo.ImageSharingMode = SharingMode.Exclusive;
                creatInfo.QueueFamilyIndexCount = 0;
            }

            creatInfo = creatInfo with
            {
                PreTransform = swapchainSupport.Capabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = presentMode,
                Clipped = true,
                //OldSwapchain = 0,
            };

            if (swapchain.KhrSwapchain == null)
            {
                if (!context.Vk!.TryGetDeviceExtension(context.Instance, context.Device.LogicalDevice, out KhrSwapchain khrSwapchain))
                {
                    throw new NotSupportedException("VK_KHR_swapchain extension not found.");
                }
                swapchain.KhrSwapchain = khrSwapchain;
            }

            if (swapchain.KhrSwapchain.CreateSwapchain(context.Device.LogicalDevice, creatInfo, context.Allocator, out SwapchainKHR swapchainKhr) != Result.Success)
            {
                throw new Exception("Failed to create swap chain!");
            }

            swapchain.Swapchain = swapchainKhr;

            //Images
            uint swapImageCount = 0;

            swapchain.KhrSwapchain.GetSwapchainImages(context.Device.LogicalDevice, swapchain.Swapchain, ref swapImageCount, null);
            if(swapchain.SwapchainImages == null)
            {
                swapchain.SwapchainImages = new Silk.NET.Vulkan.Image[swapImageCount];
            }
            if (swapchain.ImageViews == null)
            {
                swapchain.ImageViews = new ImageView[swapImageCount];
            }
            fixed (Silk.NET.Vulkan.Image* swapChainImagesPtr = swapchain.SwapchainImages)
            {
                swapchain.KhrSwapchain.GetSwapchainImages(context.Device.LogicalDevice, swapchain.Swapchain, ref swapImageCount, swapChainImagesPtr);
            }

            //views
            for(int cnt = 0; cnt < swapImageCount; cnt++)
            {
                ImageViewCreateInfo createInfo = new()
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = swapchain.SwapchainImages[cnt],
                    ViewType = ImageViewType.Type2D,
                    Format = swapchain.ImageFormat.Format,
                    SubresourceRange =
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                    }
                };

                if (context.Vk!.CreateImageView(context.Device.LogicalDevice, createInfo, context.Allocator, out ImageView imageView) != Result.Success)
                {
                    throw new Exception("Failed to create image views!");
                }
                swapchain.ImageViews[cnt] = imageView;
            }

            //Depth resources
            _vulkanDeviceSetup.DetectDepthFormat(context);

            //create depth image and its view
            swapchain.DepthAttachment = _vulkanImageSetup.ImageCreate(context, ImageType.Type2D, size, context.Device.DepthFormat, ImageTiling.Optimal, 
                ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, true, ImageAspectFlags.DepthBit);

            _logger.LogDebug("Swapchain created successfully!");
            context.SetupSwapchain(swapchain);
            return swapchain;
        }

        private void InnerDestroy(VulkanContext context, VulkanSwapchain swapchain)
        {
            _vulkanImageSetup.ImageDestroy(context, swapchain.DepthAttachment);

            if (swapchain.ImageViews != null)
            {
                //only destroy the views, not the images, since those aer owned by the swapchain and are this destroyed when it is.
                for (int cnt = 0; cnt < swapchain.ImageViews.Length; cnt++)
                {
                    context.Vk.DestroyImageView(context.Device.LogicalDevice, swapchain.ImageViews[cnt], context.Allocator);
                }
                swapchain.ImageViews = null;
            }

            swapchain.KhrSwapchain.DestroySwapchain(context.Device.LogicalDevice, swapchain.Swapchain, context.Allocator);
            _logger.LogDebug("Swapchain destroyed.");
        }

        /// <summary>
        /// Get preferred format.
        /// </summary>
        /// <param name="availableFormats"></param>
        /// <returns></returns>
        private SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
        {
            foreach (var availableFormat in availableFormats)
            {
                //preferred formats
                if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
                {
                    return availableFormat;
                }
            }

            return availableFormats[0];
        }

        /// <summary>
        /// Get preferred present mode.
        /// </summary>
        /// <param name="availablePresentModes"></param>
        /// <returns></returns>
        private PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
        {
            foreach (var availablePresentMode in availablePresentModes)
            {
                if (availablePresentMode == PresentModeKHR.MailboxKhr)
                {
                    return availablePresentMode;
                }
            }

            return PresentModeKHR.FifoKhr;
        }
    }
}
