using DragonGameEngine.Core.Exceptions.Vulkan;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;

namespace DragonGameEngine.Core.Rendering.Vulkan
{
    public unsafe sealed class VulkanRenderpassSetup
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
        public VulkanRenderpass Create(VulkanContext context, Rect2D rect, System.Drawing.Color color, float depth, uint stencil, 
            RenderpassClearFlags clearFlags, bool hasPreviousPass, bool hasNextPass)
        {

            //attachments TODO: make configurable
            var attachmentDescriptions = new List<AttachmentDescription>(2);
            
            //Color attachment
            bool doClearColor = (clearFlags & RenderpassClearFlags.ClearColorBufferFlag) != 0;

            AttachmentDescription colorAttachment = new()
            {
                Format = context.Swapchain!.ImageFormat.Format, //TODO: configurable
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = doClearColor ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = hasPreviousPass ? ImageLayout.ColorAttachmentOptimal : ImageLayout.Undefined,
                FinalLayout = hasNextPass ? ImageLayout.ColorAttachmentOptimal : ImageLayout.PresentSrcKhr,
            };
            attachmentDescriptions.Add(colorAttachment);

            AttachmentReference colorAttachmentRef = new()
            {
                Attachment = 0, //array index
                Layout = ImageLayout.ColorAttachmentOptimal,
            };

            //main subpass
            SubpassDescription subpass = new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef,
            };

            //depth attachment
            AttachmentReference depthAttachmentRef = new()
            {
                Attachment = 1,
                Layout = ImageLayout.DepthStencilAttachmentOptimal,
            };
            bool doClearDepth = (clearFlags & RenderpassClearFlags.ClearDepthBufferFlag) != 0;
            if(doClearDepth)
            {
                AttachmentDescription depthAttachment = new()
                {
                    Format = context.Device.DepthFormat,
                    Samples = SampleCountFlags.Count1Bit,
                    LoadOp = doClearDepth ? AttachmentLoadOp.Clear : AttachmentLoadOp.Load,
                    StoreOp = AttachmentStoreOp.DontCare,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
                };
                attachmentDescriptions.Add(depthAttachment);

                subpass.PDepthStencilAttachment = &depthAttachmentRef;
            }

            SubpassDependency dependency = new()
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
            };

            var attachments = attachmentDescriptions.ToArray();

            RenderPass renderPass;
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

                var createRenderPassResult = context.Vk.CreateRenderPass(context.Device.LogicalDevice, renderPassInfo, context.Allocator, out renderPass);
                if (createRenderPassResult != Result.Success)
                {
                    throw new VulkanResultException(createRenderPassResult, "Failed to create render pass!");
                }
            }

            var vulkanRenderpass = new VulkanRenderpass()
            {
                Rect = rect,
                Color = color,
                Depth = depth,
                Stencil = stencil,
                Handle = renderPass,
                ClearFlags = clearFlags,
                HasPreviousPass = hasPreviousPass,
                HasNextPass = hasNextPass,
                State = RenderpassState.Ready,
            };

            _logger.LogDebug("Render pass created!");
            return vulkanRenderpass;
        }

        public void Destory(VulkanContext context, VulkanRenderpass renderpass)
        {
            if (renderpass.Handle.Handle != 0)
            {
                context.Vk.DestroyRenderPass(context.Device.LogicalDevice, renderpass.Handle, context.Allocator);
                //renderpass.Handle = default;
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

            //cache this
            var clearValues = new List<ClearValue>(2);

            bool doClearColor = (vulkanRenderpass.ClearFlags & RenderpassClearFlags.ClearColorBufferFlag) != 0;
            if(doClearColor)
            {
                clearValues.Add(new()
                {
                    Color = new() { Float32_0 = vulkanRenderpass.Color.R / 255f, Float32_1 = vulkanRenderpass.Color.G / 255f, Float32_2 = vulkanRenderpass.Color.B / 255f, Float32_3 = vulkanRenderpass.Color.A / 255f },
                });
            }

            bool doClearDepth= (vulkanRenderpass.ClearFlags & RenderpassClearFlags.ClearDepthBufferFlag) != 0;
            bool doClearStencil = (vulkanRenderpass.ClearFlags & RenderpassClearFlags.ClearStencilBufferFlag) != 0;
            if (doClearDepth)
            {
                clearValues.Add(new()
                {
                    DepthStencil = new() 
                    { 
                        Depth = vulkanRenderpass.Depth, 
                        Stencil = doClearStencil ? vulkanRenderpass.Stencil : 0
                    }
                });
            }

            var clearValuesArray = clearValues.ToArray();
            fixed (ClearValue* clearValuesPtr = clearValuesArray)
            {
                renderPassInfo.ClearValueCount = (uint)clearValuesArray.Length;
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
