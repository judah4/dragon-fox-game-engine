using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace DragonFoxGameEngine.Core.InitialPrototype.Vulkan
{
    public struct Vertex
    {
        public Vector3D<float> pos;
        public Vector3D<float> color;
        public Vector2D<float> textCoord;

        public static VertexInputBindingDescription GetBindingDescription()
        {
            VertexInputBindingDescription bindingDescription = new()
            {
                Binding = 0,
                Stride = (uint)Unsafe.SizeOf<Vertex>(),
                InputRate = VertexInputRate.Vertex,
            };

            return bindingDescription;
        }

        public static VertexInputAttributeDescription[] GetAttributeDescriptions()
        {
            var attributeDescriptions = new[]
            {
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(pos)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(color)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(textCoord)),
            }
        };

            return attributeDescriptions;
        }
    }

    /// <summary>
    /// Vulkan graphics pipeline data
    /// </summary>
    public readonly struct VulkanPipelineData
    {
        /// <summary>
        /// The internal pipeline handle.
        /// </summary>
        public readonly Pipeline PipelineHandle;
        /// <summary>
        /// The pipeline layout.
        /// </summary>
        public readonly PipelineLayout PipelineLayout;

        public VulkanPipelineData(Pipeline graphicsPipeline, PipelineLayout pipelineLayout)
        {
            PipelineHandle = graphicsPipeline;
            PipelineLayout = pipelineLayout;
        }
    }

    public unsafe class VulkanPipeline
    {
        public VulkanPipelineData CreateGraphicsWorldPipeline(Vk vk, Device device, RenderPass renderPass, DescriptorSetLayout descriptorSetLayout, Viewport viewport, Rect2D scissor)
        {
            var vertShaderCode = File.ReadAllBytes("Assets/shaders/vert.spv");
            var fragShaderCode = File.ReadAllBytes("Assets/shaders/frag.spv");

            var vertShaderModule = CreateShaderModule(vk, device, vertShaderCode);
            var fragShaderModule = CreateShaderModule(vk, device, fragShaderCode);

            PipelineShaderStageCreateInfo vertShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = vertShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

            PipelineShaderStageCreateInfo fragShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

            var shaderStages = stackalloc[]
            {
                vertShaderStageInfo,
                fragShaderStageInfo
            };

            var pipeline = CreateGraphicsPipeline(vk, device, renderPass, descriptorSetLayout, shaderStages, viewport, scissor);

            vk!.DestroyShaderModule(device, fragShaderModule, null);
            vk!.DestroyShaderModule(device, vertShaderModule, null);

            SilkMarshal.Free((nint)vertShaderStageInfo.PName);
            SilkMarshal.Free((nint)fragShaderStageInfo.PName);
            return pipeline;
        }

        public VulkanPipelineData CreateGraphicsUiPipeline(Vk vk, Device device, RenderPass renderPass, DescriptorSetLayout descriptorSetLayout, Viewport viewport, Rect2D scissor)
        {
            var vertShaderCode = File.ReadAllBytes("Assets/shaders/Builtin.UIShader.vert.spv");
            var fragShaderCode = File.ReadAllBytes("Assets/shaders/Builtin.UIShader.frag.spv");

            var vertShaderModule = CreateShaderModule(vk, device, vertShaderCode);
            var fragShaderModule = CreateShaderModule(vk, device, fragShaderCode);

            PipelineShaderStageCreateInfo vertShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = vertShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("ui")
            };

            PipelineShaderStageCreateInfo fragShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragShaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("ui")
            };

            var shaderStages = stackalloc[]
            {
                vertShaderStageInfo,
                fragShaderStageInfo
            };

            var pipeline = CreateGraphicsPipeline(vk, device, renderPass, descriptorSetLayout, shaderStages, viewport, scissor);

            vk!.DestroyShaderModule(device, fragShaderModule, null);
            vk!.DestroyShaderModule(device, vertShaderModule, null);

            SilkMarshal.Free((nint)vertShaderStageInfo.PName);
            SilkMarshal.Free((nint)fragShaderStageInfo.PName);
            return pipeline;
        }

        public VulkanPipelineData CreateGraphicsPipeline(Vk vk, Device device, RenderPass renderPass, DescriptorSetLayout descriptorSetLayout, PipelineShaderStageCreateInfo* shaderStages, Viewport viewport, Rect2D scissor)
        {

            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();

            fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
            {
                DescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout;

                PipelineVertexInputStateCreateInfo vertexInputInfo = new()
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = 1,
                    VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                    PVertexBindingDescriptions = &bindingDescription,
                    PVertexAttributeDescriptions = attributeDescriptionsPtr,
                };

                PipelineInputAssemblyStateCreateInfo inputAssembly = new()
                {
                    SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                    Topology = PrimitiveTopology.TriangleList,
                    PrimitiveRestartEnable = false,
                };

                PipelineViewportStateCreateInfo viewportState = new()
                {
                    SType = StructureType.PipelineViewportStateCreateInfo,
                    ViewportCount = 1,
                    PViewports = &viewport,
                    ScissorCount = 1,
                    PScissors = &scissor,
                };

                PipelineRasterizationStateCreateInfo rasterizer = new()
                {
                    SType = StructureType.PipelineRasterizationStateCreateInfo,
                    DepthClampEnable = false,
                    RasterizerDiscardEnable = false,
                    PolygonMode = PolygonMode.Fill,
                    LineWidth = 1,
                    CullMode = CullModeFlags.BackBit,
                    FrontFace = FrontFace.CounterClockwise,
                    DepthBiasEnable = false,
                };

                PipelineMultisampleStateCreateInfo multisampling = new()
                {
                    SType = StructureType.PipelineMultisampleStateCreateInfo,
                    SampleShadingEnable = false,
                    RasterizationSamples = SampleCountFlags.Count1Bit,
                };

                PipelineDepthStencilStateCreateInfo depthStencil = new()
                {
                    SType = StructureType.PipelineDepthStencilStateCreateInfo,
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthCompareOp = CompareOp.Less,
                    DepthBoundsTestEnable = false,
                    StencilTestEnable = false,
                };

                PipelineColorBlendAttachmentState colorBlendAttachment = new()
                {
                    ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                    BlendEnable = false,
                };

                PipelineColorBlendStateCreateInfo colorBlending = new()
                {
                    SType = StructureType.PipelineColorBlendStateCreateInfo,
                    LogicOpEnable = false,
                    LogicOp = LogicOp.Copy,
                    AttachmentCount = 1,
                    PAttachments = &colorBlendAttachment,
                };

                colorBlending.BlendConstants[0] = 0;
                colorBlending.BlendConstants[1] = 0;
                colorBlending.BlendConstants[2] = 0;
                colorBlending.BlendConstants[3] = 0;

                PipelineLayoutCreateInfo pipelineLayoutInfo = new()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    PushConstantRangeCount = 0,
                    SetLayoutCount = 1,
                    PSetLayouts = descriptorSetLayoutPtr
                };

                if (vk.CreatePipelineLayout(device, pipelineLayoutInfo, null, out var pipelineLayout) != Result.Success)
                {
                    throw new Exception("failed to create pipeline layout!");
                }

                GraphicsPipelineCreateInfo pipelineInfo = new()
                {
                    SType = StructureType.GraphicsPipelineCreateInfo,
                    StageCount = 2,
                    PStages = shaderStages,
                    PVertexInputState = &vertexInputInfo,
                    PInputAssemblyState = &inputAssembly,
                    PViewportState = &viewportState,
                    PRasterizationState = &rasterizer,
                    PMultisampleState = &multisampling,
                    PDepthStencilState = &depthStencil,
                    PColorBlendState = &colorBlending,
                    Layout = pipelineLayout,
                    RenderPass = renderPass,
                    Subpass = 0,
                    BasePipelineHandle = default
                };

                if (vk.CreateGraphicsPipelines(device, default, 1, pipelineInfo, null, out var graphicsPipeline) != Result.Success)
                {
                    throw new Exception("failed to create graphics pipeline!");
                }

                return new VulkanPipelineData(graphicsPipeline, pipelineLayout);
            }
        }

        private ShaderModule CreateShaderModule(Vk vk, Device device, byte[] code)
        {
            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)code.Length,
            };

            ShaderModule shaderModule;

            fixed (byte* codePtr = code)
            {
                createInfo.PCode = (uint*)codePtr;

                if (vk.CreateShaderModule(device, createInfo, null, out shaderModule) != Result.Success)
                {
                    throw new Exception();
                }
            }

            return shaderModule;
        }

        public void CleanUpPipeline(Vk vk, Device device, VulkanPipelineData pipelineData)
        {
            vk.DestroyPipeline(device, pipelineData.PipelineHandle, null);
            vk.DestroyPipelineLayout(device, pipelineData.PipelineLayout, null);
        }
    }
}
