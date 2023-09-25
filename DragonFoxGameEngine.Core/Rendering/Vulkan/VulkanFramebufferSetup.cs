using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanFramebufferSetup
    {
        ILogger _logger;

        public VulkanFramebufferSetup(ILogger logger)
        {
            _logger = logger;
        }

        public VulkanFramebuffer FramebufferCreate(VulkanContext context, VulkanRenderpass vulkanRenderpass, Vector2D<uint> size, ImageView[] attachments)
        {
            VulkanFramebuffer vulkanFramebuffer = new VulkanFramebuffer()
            {
                Renderpass = vulkanRenderpass,
                Attachments = new ImageView[attachments.Length],
            };
            for(int cnt = 0; cnt < attachments.Length; cnt++)
            {
                vulkanFramebuffer.Attachments[cnt] = attachments[cnt];
            }

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

                if (context.Vk.CreateFramebuffer(context.Device.LogicalDevice, framebufferInfo, context.Allocator, out var framebuffer) != Result.Success)
                {
                    throw new Exception("Failed to create framebuffer!");
                }
                vulkanFramebuffer.Framebuffer = framebuffer;
            }
            _logger.LogDebug($"Framebuffer created");
            return vulkanFramebuffer;
        }

        public VulkanFramebuffer FramebufferDestroy(VulkanContext context, VulkanFramebuffer vulkanFramebuffer)
        {
            context.Vk.DestroyFramebuffer(context.Device.LogicalDevice, vulkanFramebuffer.Framebuffer, context.Allocator);
            vulkanFramebuffer.Attachments = new ImageView[0];
            vulkanFramebuffer.Framebuffer = default;
            vulkanFramebuffer.Renderpass = default;
            _logger.LogDebug($"Framebuffer destroyed");
            return vulkanFramebuffer;
        }
    }
}
