using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct VulkanSwapchain
    {
        public SurfaceFormatKHR ImageFormat;
        public KhrSwapchain KhrSwapchain;
        public SwapchainKHR Swapchain;
        public byte MaxFramesInFlight;
        public Image[] SwapchainImages;
        public ImageView[]? ImageViews;
        internal VulkanImage DepthAttachment;

        public VulkanFramebuffer[] Framebuffers;

        //public Format swapChainImageFormat;
        //public Extent2D swapChainExtent;
        //public Framebuffer[]? _uiFramebuffers;
    }
}
