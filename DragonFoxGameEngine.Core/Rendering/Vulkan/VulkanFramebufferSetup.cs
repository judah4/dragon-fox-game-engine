using DragonGameEngine.Core.Exceptions.Vulkan;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan
{
    public unsafe sealed class VulkanFramebufferSetup
    {
        private readonly ILogger _logger;

        public VulkanFramebufferSetup(ILogger logger)
        {
            _logger = logger;
        }

        public Framebuffer FramebufferCreate(VulkanContext context, VulkanRenderpass vulkanRenderpass, Vector2D<uint> size, ImageView[] attachments)
        {
            fixed (ImageView* attachmentsPtr = attachments)
            {
                FramebufferCreateInfo framebufferInfo = new()
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = vulkanRenderpass.Handle,
                    AttachmentCount = (uint)attachments.Length,
                    PAttachments = attachmentsPtr,
                    Width = size.X,
                    Height = size.Y,
                    Layers = 1,
                };

                var frameBufferResult = context.Vk.CreateFramebuffer(context.Device.LogicalDevice, framebufferInfo, context.Allocator, out var framebuffer);
                if (frameBufferResult != Result.Success)
                {
                    throw new VulkanResultException(frameBufferResult, "Failed to create framebuffer!");
                }
                _logger.LogDebug($"Framebuffer created");
                return framebuffer;
            }
        }

        public Framebuffer FramebufferDestroy(VulkanContext context, Framebuffer framebuffer)
        {
            context.Vk.DestroyFramebuffer(context.Device.LogicalDevice, framebuffer, context.Allocator);
            _logger.LogDebug($"Framebuffer destroyed");
            return framebuffer;
        }
    }
}
