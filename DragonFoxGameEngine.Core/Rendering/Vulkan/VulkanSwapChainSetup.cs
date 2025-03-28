﻿using DragonGameEngine.Core.Exceptions.Vulkan;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.OpenAL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;

namespace DragonGameEngine.Core.Rendering.Vulkan
{
    public unsafe sealed class VulkanSwapchainSetup
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
            return InnerCreate(context, size, InitialCreate(context));
        }

        public VulkanSwapchain Recreate(VulkanContext context, Vector2D<uint> size, VulkanSwapchain swapchain)
        {
            swapchain = InnerDestroy(context, swapchain);
            return InnerCreate(context, size, swapchain);
        }

        public VulkanSwapchain Destroy(VulkanContext context, VulkanSwapchain swapchain)
        {
            context.Vk.DeviceWaitIdle(context.Device.LogicalDevice);
            return InnerDestroy(context, swapchain);
        }

        /// <summary>
        /// Aquire the next image index
        /// </summary>
        /// <param name="context"></param>
        /// <param name="swapchain"></param>
        /// <param name="timeoutNs"></param>
        /// <param name="semaphore"></param>
        /// <param name="fence"></param>
        /// <returns>The image index</returns>
        /// <exception cref="Exception">Throws if not successful.</exception>
        public uint AquireNextImageIndex(VulkanContext context, VulkanSwapchain swapchain, ulong timeoutNs, Semaphore semaphore, Fence fence)
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
                throw new VulkanResultException(result, "failed to acquire swap chain image!");
            }

            return imageIndex;
        }

        public void Present(VulkanContext context, VulkanSwapchain swapchain, Queue graphicsQueue, Queue presentQueue, Semaphore* renderCompleteSemaphore, uint presentImageIndex)
        {
            var swapchainKhr = swapchain.Swapchain;
            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,

                WaitSemaphoreCount = 1,
                PWaitSemaphores = renderCompleteSemaphore,

                SwapchainCount = 1,
                PSwapchains = &swapchainKhr,

                PImageIndices = &presentImageIndex,
            };

            var result = swapchain.KhrSwapchain.QueuePresent(presentQueue, presentInfo);

            if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr)
            {
                Recreate(context, context.FramebufferSize, swapchain);
            }
            else if (result != Result.Success)
            {
                throw new VulkanResultException(result, "Failed to present swap chain image!");
            }

            //Increment and loop the index
            var currentFrame = (context.CurrentFrame + 1) % swapchain.MaxFramesInFlight;
            context.SetCurrentFrame(currentFrame);
        }

        private VulkanSwapchain InitialCreate(VulkanContext context)
        {
            if (!context.Vk.TryGetDeviceExtension(context.Instance, context.Device.LogicalDevice, out KhrSwapchain khrSwapchain))
            {
                throw new NotSupportedException("VK_KHR_swapchain extension not found.");
            }

            return new VulkanSwapchain(khrSwapchain);
        }

        private VulkanSwapchain InnerCreate(VulkanContext context, Vector2D<uint> size, VulkanSwapchain swapchain)
        {
            var swapchainExtent = new Extent2D(size.X, size.Y);
            swapchain.MaxFramesInFlight = 2;

            swapchain.ImageFormat = ChooseSwapSurfaceFormat(context.Device.SwapchainSupport.Formats);
            var presentMode = ChoosePresentMode(context.Device.SwapchainSupport.PresentModes);

            var swapchainSupport = _vulkanDeviceSetup.QuerySwapChainSupport(context.Device.PhysicalDevice, context);

            if (swapchainSupport.Capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                swapchainExtent = swapchainSupport.Capabilities.CurrentExtent;
            }

            //clamp to the value allowed by the GPU
            var min = swapchainSupport.Capabilities.MinImageExtent;
            var max = swapchainSupport.Capabilities.MaxImageExtent;
            swapchainExtent.Width = Math.Clamp(swapchainExtent.Width, min.Width, max.Width);
            swapchainExtent.Height = Math.Clamp(swapchainExtent.Height, min.Height, max.Height);

            var imageCount = swapchainSupport.Capabilities.MinImageCount + 1;
            if (swapchainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapchainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapchainSupport.Capabilities.MaxImageCount;
            }

            SwapchainCreateInfoKHR createInfo = new()
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
                createInfo = createInfo with
                {
                    ImageSharingMode = SharingMode.Concurrent,
                    QueueFamilyIndexCount = 2,
                    PQueueFamilyIndices = queueFamilyIndices,
                };
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
                createInfo.QueueFamilyIndexCount = 0;
            }

            createInfo = createInfo with
            {
                PreTransform = swapchainSupport.Capabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = presentMode,
                Clipped = true,
                //OldSwapchain = 0,
            };

            var createSwapchainResult = swapchain.KhrSwapchain.CreateSwapchain(context.Device.LogicalDevice, createInfo, context.Allocator, out SwapchainKHR swapchainKhr);
            if (createSwapchainResult != Result.Success)
            {
                throw new VulkanResultException(createSwapchainResult, "Failed to create swap chain!");
            }

            swapchain.Swapchain = swapchainKhr;

            //Images
            uint swapImageCount = 0;

            swapchain.KhrSwapchain.GetSwapchainImages(context.Device.LogicalDevice, swapchain.Swapchain, ref swapImageCount, null);
            if (swapchain.SwapchainImages == null)
            {
                swapchain.SwapchainImages = new Image[swapImageCount];
            }
            if (swapchain.ImageViews == null)
            {
                swapchain.ImageViews = new ImageView[swapImageCount];
            }
            fixed (Image* swapChainImagesPtr = swapchain.SwapchainImages)
            {
                swapchain.KhrSwapchain.GetSwapchainImages(context.Device.LogicalDevice, swapchain.Swapchain, ref swapImageCount, swapChainImagesPtr);
            }

            //views
            for (int cnt = 0; cnt < swapImageCount; cnt++)
            {
                ImageViewCreateInfo imageCreateInfo = new()
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

                var createResult = context.Vk!.CreateImageView(context.Device.LogicalDevice, imageCreateInfo, context.Allocator, out ImageView imageView);
                if (createResult != Result.Success)
                {
                    throw new VulkanResultException(createResult, "Failed to create image views!");
                }
                swapchain.ImageViews[cnt] = imageView;
            }

            //Depth resources
            _vulkanDeviceSetup.DetectDepthFormat(context);

            //create depth image and its view
            swapchain.DepthAttachment = _vulkanImageSetup.ImageCreate(
                context,
                ImageType.Type2D,
                size,
                1U,
                context.Device.DepthFormat,
                ImageTiling.Optimal,
                ImageUsageFlags.DepthStencilAttachmentBit,
                MemoryPropertyFlags.DeviceLocalBit,
                true,
                ImageAspectFlags.DepthBit);

            _logger.LogDebug("Swapchain created successfully!");
            context.SetupSwapchain(swapchain);
            return swapchain;
        }

        private VulkanSwapchain InnerDestroy(VulkanContext context, VulkanSwapchain swapchain)
        {
            swapchain.DepthAttachment = _vulkanImageSetup.ImageDestroy(context, swapchain.DepthAttachment);

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
            return swapchain;
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
