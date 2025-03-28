﻿using DragonGameEngine.Core.Exceptions.Vulkan;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace DragonGameEngine.Core.Rendering.Vulkan
{
    public unsafe sealed class VulkanPipelineSetup
    {
        private readonly ILogger _logger;

        public VulkanPipelineSetup(ILogger logger)
        {
            _logger = logger;
        }

        public VulkanPipeline PipelineCreate(VulkanContext context,
            VulkanRenderpass renderpass,
            uint stride,
            VertexInputAttributeDescription[] attributes,
            DescriptorSetLayout[] descriptorSetLayouts,
            PipelineShaderStageCreateInfo[] stages,
            Viewport viewport,
            Rect2D scissor,
            bool isWireFrame,
            bool depthTestEnabled
            )
        {
            //viewport
            PipelineViewportStateCreateInfo viewportState = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                PViewports = &viewport,
                ScissorCount = 1,
                PScissors = &scissor,
            };

            //Resterizer
            PipelineRasterizationStateCreateInfo rasterizer = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = isWireFrame ? PolygonMode.Line : PolygonMode.Fill,
                LineWidth = 1.0f,
                CullMode = CullModeFlags.BackBit,
                FrontFace = FrontFace.CounterClockwise,
                DepthBiasEnable = false,
                DepthBiasConstantFactor = 0.0f,
                DepthBiasClamp = 0.0f,
                DepthBiasSlopeFactor = 0.0f,
            };

            //multisampling
            PipelineMultisampleStateCreateInfo multisampling = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = false,
                RasterizationSamples = SampleCountFlags.Count1Bit,
                MinSampleShading = 1.0f,
                //PSampleMask = default,
                AlphaToCoverageEnable = false,
                AlphaToOneEnable = false,
            };

            //Depth and stencil testing.
            PipelineDepthStencilStateCreateInfo depthStencil = default;
            if (depthTestEnabled)
            {
                depthStencil = new PipelineDepthStencilStateCreateInfo()
                {
                    SType = StructureType.PipelineDepthStencilStateCreateInfo,
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthCompareOp = CompareOp.Less,
                    DepthBoundsTestEnable = false,
                    StencilTestEnable = false,
                };
            }

            PipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                BlendEnable = true,
                SrcColorBlendFactor = BlendFactor.SrcAlpha,
                DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = BlendFactor.SrcAlpha,
                DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
                AlphaBlendOp = BlendOp.Add,

                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            };

            PipelineColorBlendStateCreateInfo colorBlending = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy,
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachment,
            };

            //dynamic state
            var dynamicStates = new DynamicState[]
            {
                DynamicState.Viewport,
                DynamicState.Scissor,
                DynamicState.LineWidth,
            };

            fixed (DescriptorSetLayout* descriptorSetLayoutsPtr = descriptorSetLayouts)
            fixed (VertexInputAttributeDescription* attributesPtr = attributes)
            fixed (DynamicState* dynamicStatesPtr = dynamicStates)
            fixed (PipelineShaderStageCreateInfo* stagesPtr = stages)
            {
                var dynamicStateCreateInfo = new PipelineDynamicStateCreateInfo()
                {
                    SType = StructureType.PipelineDynamicStateCreateInfo,
                    DynamicStateCount = (uint)dynamicStates.Length,
                    PDynamicStates = dynamicStatesPtr,
                };

                //Vertex input
                var bindingDescription = new VertexInputBindingDescription()
                {
                    Binding = 0, //Binding index
                    Stride = stride,
                    InputRate = VertexInputRate.Vertex, //move to next data entry for each vertext
                };

                //attributes
                PipelineVertexInputStateCreateInfo vertexInputInfo = new()
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = 1,
                    PVertexBindingDescriptions = &bindingDescription,
                    VertexAttributeDescriptionCount = (uint)attributes.Length,
                    PVertexAttributeDescriptions = attributesPtr,
                };

                //input assembly
                PipelineInputAssemblyStateCreateInfo inputAssembly = new()
                {
                    SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                    Topology = PrimitiveTopology.TriangleList,
                    PrimitiveRestartEnable = false,
                };

                // Descriptor set layouts
                PipelineLayoutCreateInfo pipelineLayoutInfo = new()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    SetLayoutCount = (uint)descriptorSetLayouts.Length,
                    PSetLayouts = descriptorSetLayoutsPtr,
                };

                //Push constants
                PushConstantRange pushConstant = new PushConstantRange()
                {
                    StageFlags = ShaderStageFlags.VertexBit,
                    Offset = (uint)sizeof(Matrix4X4<float>) * 0,
                    Size = (uint)sizeof(Matrix4X4<float>) * 2,
                };

                pipelineLayoutInfo.PushConstantRangeCount = 1;
                pipelineLayoutInfo.PPushConstantRanges = &pushConstant;

                //pipeline layout
                var createPipeline = context.Vk.CreatePipelineLayout(context.Device.LogicalDevice, pipelineLayoutInfo, context.Allocator, out var pipelineLayout);
                if (createPipeline != Result.Success)
                {
                    throw new VulkanResultException(createPipeline, "Failed to create pipeline layout!");
                }

                //Pipeline create
                GraphicsPipelineCreateInfo pipelineCreateInfo = new()
                {
                    SType = StructureType.GraphicsPipelineCreateInfo,
                    StageCount = (uint)stages.Length,
                    PStages = stagesPtr,
                    PVertexInputState = &vertexInputInfo,
                    PInputAssemblyState = &inputAssembly,
                    PViewportState = &viewportState,
                    PRasterizationState = &rasterizer,
                    PMultisampleState = &multisampling,
                    PDepthStencilState = depthTestEnabled ? &depthStencil : default,
                    PColorBlendState = &colorBlending,
                    PDynamicState = &dynamicStateCreateInfo,
                    PTessellationState = default,
                    Layout = pipelineLayout,
                    RenderPass = renderpass.Handle,
                    Subpass = 0,
                    BasePipelineHandle = default,
                    BasePipelineIndex = -1,
                };

                var createGraphicsResult = context.Vk.CreateGraphicsPipelines(context.Device.LogicalDevice, default, 1, pipelineCreateInfo, context.Allocator, out var graphicsPipeline);
                if (createGraphicsResult != Result.Success)
                {
                    throw new VulkanResultException(createGraphicsResult, "failed to create graphics pipeline!");
                }

                var vulkanPipeline = new VulkanPipeline()
                {
                    Handle = graphicsPipeline,
                    PipelineLayout = pipelineLayout,
                };
                _logger.LogDebug("Graphics pipeline created!");
                return vulkanPipeline;
            }
        }

        public VulkanPipeline PipelineDestroy(VulkanContext context, VulkanPipeline vulkanPipeline)
        {
            context.Vk.DestroyPipeline(context.Device.LogicalDevice, vulkanPipeline.Handle, context.Allocator);
            context.Vk.DestroyPipelineLayout(context.Device.LogicalDevice, vulkanPipeline.PipelineLayout, context.Allocator);
            vulkanPipeline = default;
            return vulkanPipeline;
        }

        public void PipelineBind(VulkanContext context, VulkanCommandBuffer commandBuffer, PipelineBindPoint bindPoint, VulkanPipeline vulkanPipeline)
        {
            context.Vk.CmdBindPipeline(commandBuffer.Handle, bindPoint, vulkanPipeline.Handle);
        }
    }
}
