using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenAL;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace DragonGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanRenderpassSetup
    {
        private readonly ILogger _logger;

        public VulkanRenderpassSetup(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        /// <param name="depth"></param>
        /// <param name="stencil"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <remarks>Impure, set context MainRenderpass.</remarks>
        public VulkanRenderpass Create(VulkanContext context, Rect2D rect, System.Drawing.Color color, float depth, uint stencil)
        {
            var vulkanRenderpass = new VulkanRenderpass()
            {
                Rect = rect,
                Color = color,
                Depth = depth,
                Stencil = stencil,
            };

            AttachmentDescription colorAttachment = new()
            {
                Format = context.Swapchain.ImageFormat.Format, //TODO: configurable
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr,
            };

            //attachments TODO: make configurable
            AttachmentReference colorAttachmentRef = new()
            {
                Attachment = 0, //array index
                Layout = ImageLayout.ColorAttachmentOptimal,
            };

            AttachmentDescription depthAttachment = new()
            {
                Format = context.Device.DepthFormat,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.DontCare,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            };

            AttachmentReference depthAttachmentRef = new()
            {
                Attachment = 1,
                Layout = ImageLayout.DepthStencilAttachmentOptimal,
            };

            //main subpass
            SubpassDescription subpass = new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef,
                PDepthStencilAttachment = &depthAttachmentRef,
            };

            SubpassDependency dependency = new()
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
            };

            var attachments = new[] { colorAttachment, depthAttachment };

            fixed (AttachmentDescription* attachmentsPtr = attachments)
            {
                RenderPassCreateInfo renderPassInfo = new()
                {
                    SType = StructureType.RenderPassCreateInfo,
                    AttachmentCount = (uint)attachments.Length,
                    PAttachments = attachmentsPtr,
                    SubpassCount = 1,
                    PSubpasses = &subpass,
                    DependencyCount = 1,
                    PDependencies = &dependency,
                };

                if (context.Vk.CreateRenderPass(context.Device.LogicalDevice, renderPassInfo, context.Allocator, out var renderPass) != Result.Success)
                {
                    throw new Exception("Failed to create render pass!");
                }
                vulkanRenderpass.Handle = renderPass;
                vulkanRenderpass.State = RenderpassState.Ready;
            }

            context.SetupMainRenderpass(vulkanRenderpass);
            _logger.LogDebug("Render pass created!");
            return vulkanRenderpass;
        }

        public void Destory(VulkanContext context, VulkanRenderpass renderpass)
        {
            if (renderpass.Handle.Handle != 0)
            {
                context.Vk.DestroyRenderPass(context.Device.LogicalDevice, renderpass.Handle, context.Allocator);
                renderpass.Handle = default;
            }
            _logger.LogDebug("Render pass destroy.");
        }

        public VulkanCommandBuffer BeginRenderpass(VulkanContext context, VulkanCommandBuffer commandBuffer, VulkanRenderpass vulkanRenderpass, Framebuffer framebuffer)
        {
            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = vulkanRenderpass.Handle,
                Framebuffer = framebuffer,
                RenderArea = vulkanRenderpass.Rect, //yay objects!
            };
            var clearValues = new ClearValue[]
            {
                new()
                {
                    Color = new (){ Float32_0 = vulkanRenderpass.Color.R/255f, Float32_1 = vulkanRenderpass.Color.G/255f, Float32_2 = vulkanRenderpass.Color.B/255f, Float32_3 = vulkanRenderpass.Color.A/255f },
                },
                new()
                {
                    DepthStencil = new () { Depth = vulkanRenderpass.Depth, Stencil = vulkanRenderpass.Stencil }
                }
            };

            fixed (ClearValue* clearValuesPtr = clearValues)
            {
                renderPassInfo.ClearValueCount = (uint)clearValues.Length;
                renderPassInfo.PClearValues = clearValuesPtr;

                context.Vk.CmdBeginRenderPass(commandBuffer.Handle, &renderPassInfo, SubpassContents.Inline);
                commandBuffer.State = CommandBufferState.InRenderPass;
            }
            return commandBuffer;
        }

        public VulkanCommandBuffer EndRenderpass(VulkanContext context, VulkanCommandBuffer commandBuffer, VulkanRenderpass vulkanRenderpass)
        {
            context.Vk.CmdEndRenderPass(commandBuffer.Handle);
            commandBuffer.State = CommandBufferState.Recording;
            return commandBuffer;

        }
    }
}
