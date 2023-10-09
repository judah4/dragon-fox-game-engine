using DragonFoxGameEngine.Core.Maths;
using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain.Shaders;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenAL;
using Silk.NET.Vulkan;
using System;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Shaders
{
    public unsafe class VulkanObjectShaderSetup
    {
        public const string BUILTIN_SHADER_NAME_OBJECT = "Builtin.ObjectShader";

        private readonly ILogger _logger;
        private readonly VulkanShaderSetup _shaderSetup;
        private readonly VulkanPipelineSetup _pipelineSetup;
        private readonly VulkanBufferSetup _bufferSetup;

        public VulkanObjectShaderSetup(ILogger logger, VulkanShaderSetup shaderSetup, VulkanPipelineSetup pipelineSetup, VulkanBufferSetup bufferSetup)
        {
            _logger = logger;
            _shaderSetup = shaderSetup;
            _pipelineSetup = pipelineSetup;
            _bufferSetup = bufferSetup;
        }

        public VulkanObjectShader ObjectShaderCreate(VulkanContext context)
        {
            // Shader module init per stage
            var stageTypesNames = new string[VulkanObjectShader.OBJECT_SHADER_STAGE_COUNT] { "vert", "frag" };
            var stageTypes = new ShaderStageFlags[VulkanObjectShader.OBJECT_SHADER_STAGE_COUNT] { ShaderStageFlags.VertexBit, ShaderStageFlags.FragmentBit };

            var objectShader = new VulkanObjectShader();
            for(int cnt = 0; cnt < VulkanObjectShader.OBJECT_SHADER_STAGE_COUNT; cnt++)
            {
                var shaderModuleResult = _shaderSetup.CreateShaderModule(context, BUILTIN_SHADER_NAME_OBJECT, stageTypesNames[cnt], stageTypes[cnt], cnt);
                if(shaderModuleResult.IsFailure)
                {
                    _logger.LogError("Unable to create {typeName} shader module for {shaderName}", stageTypesNames[cnt], BUILTIN_SHADER_NAME_OBJECT);
                    throw new Exception("Failed to create shader module!");
                }
                objectShader.ShaderStages[cnt] = shaderModuleResult.Value;
            }

            //Global Descriptors

            DescriptorSetLayout globalDescriptorSetLayout = default;

            DescriptorSetLayoutBinding globalUboLayoutBinding = new()
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                PImmutableSamplers = default,
                StageFlags = ShaderStageFlags.VertexBit,
            };

            var bindings = new DescriptorSetLayoutBinding[] { globalUboLayoutBinding };

            fixed (DescriptorSetLayoutBinding* bindingsPtr = bindings)
            {
                DescriptorSetLayoutCreateInfo layoutInfo = new()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    BindingCount = (uint)bindings.Length,
                    PBindings = bindingsPtr,
                };

                if (context.Vk.CreateDescriptorSetLayout(context.Device.LogicalDevice, layoutInfo, context.Allocator, &globalDescriptorSetLayout) != Silk.NET.Vulkan.Result.Success)
                {
                    throw new Exception("Failed to create descriptor set layout!");
                }
                objectShader.GlobalDescriptorSetLayout = globalDescriptorSetLayout;
            }

            var poolSizes = new DescriptorPoolSize[]
            {
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.UniformBuffer,
                    DescriptorCount = (uint)context.Swapchain.SwapchainImages.Length,
                },
            };

            //descriptor pool
            DescriptorPool descriptorPool = default;
            fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)
            {
                DescriptorPoolCreateInfo poolInfo = new()
                {
                    SType = StructureType.DescriptorPoolCreateInfo,
                    PoolSizeCount = (uint)poolSizes.Length,
                    PPoolSizes = poolSizesPtr,
                    MaxSets = (uint)context.Swapchain.SwapchainImages.Length,
                };

                if (context.Vk.CreateDescriptorPool(context.Device.LogicalDevice, poolInfo, context.Allocator, &descriptorPool) != Silk.NET.Vulkan.Result.Success)
                {
                    throw new Exception("failed to create descriptor pool!");
                }

                objectShader.GlobalDescriptorPool = descriptorPool;
            }

            //Pipeline creation
            Viewport viewport = new()
            {
                X = 0,
                Y = context.FramebufferSize.Y,
                Width = context.FramebufferSize.X,
                Height = -context.FramebufferSize.Y, //flip to be with OpenGL
                MinDepth = 0.0f,
                MaxDepth = 1.0f,
            };

            //Scissor
            Rect2D scissor = new()
            {
                Offset = { X = 0, Y = 0 },
                Extent = new Extent2D(context.FramebufferSize.X, context.FramebufferSize.Y),
            };

            //attributes
            VertexInputAttributeDescription[] attributeDescriptions = Vertex3d.GetAttributeDescriptions();

            //Descriptor set layouts
            DescriptorSetLayout[] descriptorSetLayout = new DescriptorSetLayout[] { globalDescriptorSetLayout };

            //Stages
            //Note: should match the number of shader.stages
            var stages = new PipelineShaderStageCreateInfo[objectShader.ShaderStages.Length];
            for(int cnt = 0; cnt < objectShader.ShaderStages.Length; cnt++)
            {
                stages[cnt] = objectShader.ShaderStages[cnt].ShaderStageCreateInfo;
            }

            var vulkanPipeline = _pipelineSetup.PipelineCreate(context, context.MainRenderPass, attributeDescriptions, descriptorSetLayout, stages, viewport, scissor, false);

            objectShader.Pipeline = vulkanPipeline;

            var globalUboBuffer = _bufferSetup.BufferCreate(
                context,
                (ulong)sizeof(GlobalUniformObject),
                BufferUsageFlags.TransferDstBit | BufferUsageFlags.UniformBufferBit,
                MemoryPropertyFlags.DeviceLocalBit | MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                true);
            objectShader.GlobalUniformBuffer = globalUboBuffer;

            var globalLayouts = new DescriptorSetLayout[3];
            for(int cnt = 0; cnt < globalLayouts.Length; cnt++)
            {
                globalLayouts[cnt] = objectShader.GlobalDescriptorSetLayout;
            }

            fixed (DescriptorSetLayout* globalLayoutsPtr = globalLayouts)
            {
                DescriptorSetAllocateInfo allocateInfo = new()
                {
                    SType = StructureType.DescriptorSetAllocateInfo,
                    DescriptorPool = descriptorPool,
                    DescriptorSetCount = (uint)globalLayouts.Length,
                    PSetLayouts = globalLayoutsPtr,
                };

                fixed (DescriptorSet* descriptorSetsPtr = objectShader.GlobalDescriptorSets)
                {
                    if (context.Vk.AllocateDescriptorSets(context.Device.LogicalDevice, allocateInfo, descriptorSetsPtr) != Result.Success)
                    {
                        throw new Exception("Failed to allocate descriptor sets!");
                    }
                }
            }

            return objectShader;
        }

        public VulkanObjectShader ObjectShaderDestroy(VulkanContext context, VulkanObjectShader shader)
        {
            var logicalDevice = context.Device.LogicalDevice;

            shader.GlobalUniformBuffer = _bufferSetup.BufferDestroy(context, shader.GlobalUniformBuffer);

            shader.Pipeline = _pipelineSetup.PipelineDestroy(context, shader.Pipeline);

            context.Vk.DestroyDescriptorPool(logicalDevice, shader.GlobalDescriptorPool, context.Allocator);
            shader.GlobalDescriptorPool = default;

            context.Vk.DestroyDescriptorSetLayout(logicalDevice, shader.GlobalDescriptorSetLayout, context.Allocator);
            shader.GlobalDescriptorSetLayout = default;

            //destroy shader modules
            for (int cnt = 0; cnt <  shader.ShaderStages.Length; cnt++)
            {
                context.Vk.DestroyShaderModule(logicalDevice, shader.ShaderStages[cnt].Handle, context.Allocator);
                shader.ShaderStages[cnt].Handle = default;
            }
            return shader;
        }

        public void ObjectShaderUse(VulkanContext context, VulkanObjectShader shader)
        {
            var imageIndex = context.ImageIndex;
            _pipelineSetup.PipelineBind(context, context.GraphicsCommandBuffers![imageIndex], PipelineBindPoint.Graphics, shader.Pipeline);
        }
    }
}
