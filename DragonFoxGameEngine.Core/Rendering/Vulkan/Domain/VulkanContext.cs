using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Domain
{
    public unsafe class VulkanContext
    {
        public Vk Vk { get; }

        public IWindow Window { get; }

        public Instance Instance { get; }
        public AllocationCallbacks* Allocator { get; }

        //debugging
        public ExtDebugUtils? DebugUtils { get; }
        public DebugUtilsMessengerEXT DebugMessenger { get; }

        public KhrSurface? KhrSurface { get; private set; }
        public SurfaceKHR? Surface { get; private set; }

        public VulkanDevice Device { get; private set; }

        public Vector2D<uint> FramebufferSize { get; private set; }

        public VulkanSwapchain Swapchain { get; private set; }
        public uint ImageIndex { get; private set; }
        public uint CurrentFrame { get; private set; }
        public bool RecreatingSwapchain { get; private set; }
        public VulkanRenderpass MainRenderPass { get; private set; }

        public VulkanContext(
            Vk vk,
            IWindow window,
            Instance instance,
            AllocationCallbacks* allocator,
            ExtDebugUtils? debugUtils,
            DebugUtilsMessengerEXT debugMessenger,
            Vector2D<uint> framebufferSize)
        {
            Vk = vk;
            Window = window;
            Instance = instance;
            Allocator = allocator;
            DebugUtils = debugUtils;
            DebugMessenger = debugMessenger;
            FramebufferSize = framebufferSize;
        }

        /// <summary>
        /// For Surface setup
        /// </summary>
        /// <param name="khrSurface"></param>
        /// <param name="surface"></param>
        public void SetupSurface(KhrSurface khrSurface, SurfaceKHR surface)
        {
            KhrSurface = khrSurface;
            Surface = surface;
        }

        /// <summary>
        /// For Device setup
        /// </summary>
        /// <param name="khrSurface"></param>
        /// <param name="surface"></param>
        public void SetupDevice(VulkanDevice device)
        {
            Device = device;
        }

        /// <summary>
        /// For Swapchain setup
        /// </summary>
        /// <param name="khrSurface"></param>
        /// <param name="surface"></param>
        public void SetupSwapchain(VulkanSwapchain swapchain)
        {
            Swapchain = swapchain;
        }

        public void SetupMainRenderpass(VulkanRenderpass vulkanRenderpass)
        {
            MainRenderPass = vulkanRenderpass;
        }
    }
}
