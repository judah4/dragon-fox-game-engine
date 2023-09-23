
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public struct VulkanSwapchain
    {
        public SurfaceFormatKHR ImageFormat;
        public KhrSwapchain KhrSwapchain;
        public SwapchainKHR Swapchain;
        public byte MaxFramesInFlight;
        public Silk.NET.Vulkan.Image[] SwapchainImages;
        public ImageView[]? ImageViews;

        //public Format swapChainImageFormat;
        //public Extent2D swapChainExtent;
        //public Framebuffer[]? _swapChainFramebuffers;
        //public Framebuffer[]? _uiFramebuffers;
    }
}
