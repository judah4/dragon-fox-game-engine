using System;
using System.Collections.Generic;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public sealed class VulkanSwapchain
    {
        public SurfaceFormatKHR ImageFormat { get; set; }
        public KhrSwapchain KhrSwapchain { get; init; }
        public SwapchainKHR Swapchain { get; set; }
        public byte MaxFramesInFlight { get; private init; }
        public IReadOnlyList<Image> SwapchainImages { get; private set; }
        public IReadOnlyList<ImageView> ImageViews { get; private set; }
        public VulkanImage DepthAttachment { get; set; }

        //public Format swapChainImageFormat;
        //public Extent2D swapChainExtent;
        //public Framebuffer[]? _uiFramebuffers;

        public VulkanSwapchain(KhrSwapchain khrSwapchain)
        {
            KhrSwapchain = khrSwapchain;
            MaxFramesInFlight = 2;
            UpdateSwapchain(Array.Empty<Image>(), Array.Empty<ImageView>());
        }

        public void UpdateSwapchain(IReadOnlyList<Image> swapchainImages, IReadOnlyList<ImageView> imageViews)
        {
            SwapchainImages = swapchainImages;
            ImageViews = imageViews;
        }
    }
}
