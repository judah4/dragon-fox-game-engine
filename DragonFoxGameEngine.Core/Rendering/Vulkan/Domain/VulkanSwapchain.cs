using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct VulkanSwapchain
    {
        public SurfaceFormatKHR ImageFormat { get; set; }
        public KhrSwapchain KhrSwapchain { get; set; }
        public SwapchainKHR Swapchain { get; set; }
        public byte MaxFramesInFlight { get; set; }
        public Image[] SwapchainImages { get; set; }
        public ImageView[]? ImageViews { get; set; }
        public VulkanImage DepthAttachment { get; set; }

        public VulkanFramebuffer[] Framebuffers { get; set; }

        //public Format swapChainImageFormat;
        //public Extent2D swapChainExtent;
        //public Framebuffer[]? _uiFramebuffers;

    }
}
