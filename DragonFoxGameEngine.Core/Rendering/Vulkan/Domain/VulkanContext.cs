using DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders;
using Foxis.Library;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
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
        public ulong FramebufferSizeGeneration { get; private set; }
        public ulong FramebufferSizeGenerationLastGeneration { get; private set; }

        public VulkanSwapchain Swapchain { get; private set; }
        public VulkanRenderpass MainRenderPass { get; private set; }

        #region Buffers
        public VulkanBuffer ObjectVertexBuffer { get; private set; }
        public VulkanBuffer ObjectIndexBuffer { get; private set; }
        #endregion
        public VulkanCommandBuffer[]? GraphicsCommandBuffers { get; private set; }

        public Semaphore[]? ImageAvailableSemaphores { get; private set; }
        public Semaphore[]? QueueCompleteSemaphores { get; private set; }

        public VulkanFence[]? InFlightFences { get; private set; }

        /// <summary>
        /// Holds refs to fences which exist and are owned elsewhere.
        /// </summary>
        public VulkanFence*[]? ImagesInFlight { get; private set; }

        public uint ImageIndex { get; private set; }
        public uint CurrentFrame { get; private set; }
        public bool RecreatingSwapchain { get; private set; }

        //shaders
        public VulkanObjectShader ObjectShader { get; private set; }

        public ulong GeometryVertexOffset { get; private set; }
        public ulong GeometryIndexOffset { get; private set; }

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

        public void SetupGraphicsCommandBuffers(VulkanCommandBuffer[] commandBuffers)
        {
            GraphicsCommandBuffers = commandBuffers;
        }

        public void SetupSemaphores(Semaphore[] imageAvailableSemaphores, Semaphore[] queueCompleteSemaphores)
        {
            ImageAvailableSemaphores = imageAvailableSemaphores;
            QueueCompleteSemaphores = queueCompleteSemaphores;
        }

        public void SetupFences(VulkanFence[] inFlightFences, VulkanFence*[] imagesInFlight)
        {
            InFlightFences = inFlightFences;
            ImagesInFlight = imagesInFlight;
        }

        public void SetCurrentFrame(uint currentFrame)
        {
            CurrentFrame = currentFrame;
        }

        public void SetFramebufferSize(Vector2D<uint> size, ulong framebufferSizeGeneration)
        {
            FramebufferSize = size;
            FramebufferSizeGenerationLastGeneration = FramebufferSizeGeneration;
            FramebufferSizeGeneration = framebufferSizeGeneration;
        }

        public void SetImageIndex(uint imageIndex)
        {
            ImageIndex = imageIndex;
        }

        internal void SetRecreateSwapchain(bool recreateSwapchain)
        {
            RecreatingSwapchain = recreateSwapchain;
        }

        public void SetFramebufferSizeGenerationLastGeneration(ulong framebufferSizeGeneration)
        {
            FramebufferSizeGenerationLastGeneration = framebufferSizeGeneration;
        }

        public void SetupBuiltinShaders(VulkanObjectShader objectShader)
        {
            ObjectShader = objectShader;
        }

        public void SetupBuffers(VulkanBuffer objectVertexBuffer, VulkanBuffer objectIndexBuffer)
        {
            ObjectVertexBuffer = objectVertexBuffer;
            ObjectIndexBuffer = objectIndexBuffer;
        }

        public void SetupBufferOffsets(ulong geometryVertexOffset, ulong geometryIndexOffset)
        {
            GeometryVertexOffset = geometryVertexOffset;
            GeometryIndexOffset = geometryIndexOffset;
        }
    }
}
