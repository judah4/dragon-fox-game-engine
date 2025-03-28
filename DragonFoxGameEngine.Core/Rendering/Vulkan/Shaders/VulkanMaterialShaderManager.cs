﻿using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Exceptions.Vulkan;
using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System;

namespace DragonGameEngine.Core.Rendering.Vulkan.Shaders
{
    public unsafe sealed class VulkanMaterialShaderManager
    {
        public const string BUILTIN_SHADER_NAME_OBJECT = "Builtin.MaterialShader";

        private readonly ILogger _logger;
        private readonly VulkanShaderManager _shaderSetup;
        private readonly VulkanPipelineSetup _pipelineSetup;
        private readonly VulkanBufferManager _bufferSetup;
        private readonly TextureSystem _textureSystem;

        public VulkanMaterialShaderManager(ILogger logger, VulkanShaderManager shaderSetup, VulkanPipelineSetup pipelineSetup, VulkanBufferManager bufferSetup, TextureSystem textureSystem)
        {
            _logger = logger;
            _shaderSetup = shaderSetup;
            _pipelineSetup = pipelineSetup;
            _bufferSetup = bufferSetup;
            _textureSystem = textureSystem;
        }

        public VulkanMaterialShader Create(VulkanContext context)
        {
            // Shader module init per stage
            var stageTypesNames = new string[VulkanMaterialShader.MATERIAL_SHADER_STAGE_COUNT] { "vert", "frag" };
            var stageTypes = new ShaderStageFlags[VulkanMaterialShader.MATERIAL_SHADER_STAGE_COUNT] { ShaderStageFlags.VertexBit, ShaderStageFlags.FragmentBit };

            var materialShader = new VulkanMaterialShader();
            for (int cnt = 0; cnt < VulkanMaterialShader.MATERIAL_SHADER_STAGE_COUNT; cnt++)
            {
                var shaderModuleResult = _shaderSetup.CreateShaderModule(context, BUILTIN_SHADER_NAME_OBJECT, stageTypesNames[cnt], stageTypes[cnt], cnt);
                materialShader.ShaderStages[cnt] = shaderModuleResult;
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

            var globalBindings = new DescriptorSetLayoutBinding[] { globalUboLayoutBinding };

            fixed (DescriptorSetLayoutBinding* bindingsPtr = globalBindings)
            {
                DescriptorSetLayoutCreateInfo layoutInfo = new()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    BindingCount = (uint)globalBindings.Length,
                    PBindings = bindingsPtr,
                };

                var descriptorSetLayoutResult = context.Vk.CreateDescriptorSetLayout(context.Device.LogicalDevice, layoutInfo, context.Allocator, &globalDescriptorSetLayout);
                if (descriptorSetLayoutResult != Result.Success)
                {
                    throw new VulkanResultException(descriptorSetLayoutResult, "Failed to create global descriptor set layout!");
                }
                materialShader.GlobalDescriptorSetLayout = globalDescriptorSetLayout;
            }

            var poolSizes = new DescriptorPoolSize[]
            {
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.UniformBuffer,
                    DescriptorCount = (uint)context.Swapchain!.SwapchainImages!.Length,
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

                var createDescriptorPoolResult = context.Vk.CreateDescriptorPool(context.Device.LogicalDevice, poolInfo, context.Allocator, &descriptorPool);
                if (createDescriptorPoolResult != Result.Success)
                {
                    throw new VulkanResultException(createDescriptorPoolResult, "Failed to create descriptor pool!");
                }

                materialShader.GlobalDescriptorPool = descriptorPool;
            }

            // Sampler Uses
            materialShader.SamplerUses[0] = TextureUse.MapDiffuse;

            //Local/Object Descriptors
            var descriptorTypes = new DescriptorType[VulkanMaterialShader.DESCRIPTOR_COUNT] 
            {
                DescriptorType.UniformBuffer, //binding 0 - uniform buffer
                DescriptorType.CombinedImageSampler, //binding 1 - Diffuse sampler layout
            };

            var localBindings = new DescriptorSetLayoutBinding[descriptorTypes.Length];
            for(uint cnt = 0; cnt < descriptorTypes.Length; cnt++)
            {
                localBindings[cnt].Binding = cnt;
                localBindings[cnt].DescriptorCount = 1;
                localBindings[cnt].DescriptorType = descriptorTypes[cnt];
                localBindings[cnt].StageFlags = ShaderStageFlags.FragmentBit;
            }

            DescriptorSetLayout localDescriptorSetLayout = default;

            fixed (DescriptorSetLayoutBinding* bindingsPtr = localBindings)
            {
                DescriptorSetLayoutCreateInfo layoutInfo = new()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    BindingCount = (uint)localBindings.Length,
                    PBindings = bindingsPtr,
                };

                var descriptorSetLayoutResult = context.Vk.CreateDescriptorSetLayout(context.Device.LogicalDevice, layoutInfo, context.Allocator, &localDescriptorSetLayout);
                if (descriptorSetLayoutResult != Result.Success)
                {
                    throw new VulkanResultException(descriptorSetLayoutResult, "Failed to create local/object descriptor set layout!");
                }
                materialShader.ObjectDescriptorSetLayout = localDescriptorSetLayout;
            }

            var localPoolSizes = new DescriptorPoolSize[]
            {
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.UniformBuffer,
                    DescriptorCount = VulkanMaterialShader.MAX_MATERIAL_COUNT,
                },
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.CombinedImageSampler,
                    DescriptorCount = (uint)materialShader.SamplerUses.Length * VulkanMaterialShader.MAX_MATERIAL_COUNT,
                },
            };
            //local descriptor pool
            DescriptorPool localDescriptorPool = default;
            fixed (DescriptorPoolSize* poolSizesPtr = localPoolSizes)
            {
                DescriptorPoolCreateInfo poolInfo = new()
                {
                    SType = StructureType.DescriptorPoolCreateInfo,
                    PoolSizeCount = (uint)localPoolSizes.Length,
                    PPoolSizes = poolSizesPtr,
                    MaxSets = VulkanMaterialShader.MAX_MATERIAL_COUNT,
                    Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit,
                };

                var createDescriptorPoolResult = context.Vk.CreateDescriptorPool(context.Device.LogicalDevice, poolInfo, context.Allocator, &localDescriptorPool);
                if (createDescriptorPoolResult != Result.Success)
                {
                    throw new VulkanResultException(createDescriptorPoolResult, "Failed to create local/object descriptor pool!");
                }

                materialShader.ObjectDescriptorPool = localDescriptorPool;
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
            DescriptorSetLayout[] descriptorSetLayouts = new DescriptorSetLayout[] { globalDescriptorSetLayout, localDescriptorSetLayout };

            //Stages
            //Note: should match the number of shader.stages
            var stages = new PipelineShaderStageCreateInfo[materialShader.ShaderStages.Length];
            for (int cnt = 0; cnt < materialShader.ShaderStages.Length; cnt++)
            {
                stages[cnt] = materialShader.ShaderStages[cnt].ShaderStageCreateInfo;
            }

            var vulkanPipeline = _pipelineSetup.PipelineCreate(context, context.MainRenderPass!, (uint)sizeof(Vertex3d), attributeDescriptions, descriptorSetLayouts, stages, viewport, scissor, false, true);

            materialShader.Pipeline = vulkanPipeline;

            //create global uniform buffer
            MemoryPropertyFlags deviceLocalBits = context.Device.SupportsDeviceLocalHostVisible ? MemoryPropertyFlags.DeviceLocalBit : MemoryPropertyFlags.None;
            var globalUboBuffer = _bufferSetup.BufferCreate(
                context,
                (ulong)sizeof(VulkanMaterialShaderGlobalUniformObject),
                BufferUsageFlags.TransferDstBit | BufferUsageFlags.UniformBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit | deviceLocalBits,
                true);
            materialShader.GlobalUniformBuffer = globalUboBuffer;

            var globalLayouts = new DescriptorSetLayout[context.Swapchain.SwapchainImages.Length];
            for (int cnt = 0; cnt < globalLayouts.Length; cnt++)
            {
                globalLayouts[cnt] = materialShader.GlobalDescriptorSetLayout;
            }

            //global descriptor sets
            fixed (DescriptorSetLayout* globalLayoutsPtr = globalLayouts)
            {
                DescriptorSetAllocateInfo allocateInfo = new()
                {
                    SType = StructureType.DescriptorSetAllocateInfo,
                    DescriptorPool = descriptorPool,
                    DescriptorSetCount = (uint)globalLayouts.Length,
                    PSetLayouts = globalLayoutsPtr,
                };

                fixed (DescriptorSet* descriptorSetsPtr = materialShader.GlobalDescriptorSets)
                {
                    var allocDescriptorSetsResult = context.Vk.AllocateDescriptorSets(context.Device.LogicalDevice, allocateInfo, descriptorSetsPtr);
                    if (allocDescriptorSetsResult != Result.Success)
                    {
                        throw new VulkanResultException(allocDescriptorSetsResult, "Failed to allocate descriptor sets!");
                    }
                }
            }

            //create object uniform buffer
            var localUboBuffer = _bufferSetup.BufferCreate(
                context,
                (ulong)sizeof(VulkanMaterialShaderInstanceUniformObject) * VulkanMaterialShader.MAX_MATERIAL_COUNT,
                BufferUsageFlags.TransferDstBit | BufferUsageFlags.UniformBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                true);
            materialShader.ObjectUniformBuffer = localUboBuffer;

            return materialShader;
        }

        public VulkanMaterialShader Destroy(VulkanContext context, VulkanMaterialShader shader)
        {
            var logicalDevice = context.Device.LogicalDevice;

            context.Vk.DestroyDescriptorPool(logicalDevice, shader.ObjectDescriptorPool, context.Allocator);
            shader.ObjectDescriptorPool = default;

            context.Vk.DestroyDescriptorSetLayout(logicalDevice, shader.ObjectDescriptorSetLayout, context.Allocator);
            shader.ObjectDescriptorSetLayout = default;

            shader.GlobalUniformBuffer = _bufferSetup.BufferDestroy(context, shader.GlobalUniformBuffer);
            shader.ObjectUniformBuffer = _bufferSetup.BufferDestroy(context, shader.ObjectUniformBuffer);

            shader.Pipeline = _pipelineSetup.PipelineDestroy(context, shader.Pipeline);

            context.Vk.DestroyDescriptorPool(logicalDevice, shader.GlobalDescriptorPool, context.Allocator);
            shader.GlobalDescriptorPool = default;

            context.Vk.DestroyDescriptorSetLayout(logicalDevice, shader.GlobalDescriptorSetLayout, context.Allocator);
            shader.GlobalDescriptorSetLayout = default;

            //destroy shader modules
            for (int cnt = 0; cnt < shader.ShaderStages.Length; cnt++)
            {
                context.Vk.DestroyShaderModule(logicalDevice, shader.ShaderStages[cnt].Handle, context.Allocator);
                shader.ShaderStages[cnt].Handle = default;
            }
            return shader;
        }

        public void ShaderUse(VulkanContext context, VulkanMaterialShader shader)
        {
            var imageIndex = context.ImageIndex;
            _pipelineSetup.PipelineBind(context, context.GraphicsCommandBuffers![imageIndex], PipelineBindPoint.Graphics, shader.Pipeline);
        }

        public void UpdateGlobalState(VulkanContext context, VulkanMaterialShader shader, double deltaTime)
        {
            var imageIndex = context.ImageIndex;
            CommandBuffer commandBuffer = context.GraphicsCommandBuffers![imageIndex].Handle;
            var globalDescriptor = shader.GlobalDescriptorSets[imageIndex];

            //configure
            uint range = (uint)sizeof(VulkanMaterialShaderGlobalUniformObject);
            ulong offset = 0;

            Span<VulkanMaterialShaderGlobalUniformObject> uboData = stackalloc VulkanMaterialShaderGlobalUniformObject[1];
            uboData[0] = shader.GlobalUbo;

            //copy data
            _bufferSetup.BufferLoadData(context, shader.GlobalUniformBuffer, offset, range, 0, uboData);

            DescriptorBufferInfo bufferInfo = new()
            {
                Buffer = shader.GlobalUniformBuffer.Handle,
                Offset = offset,
                Range = range,
            };

            var descriptorWrites = new WriteDescriptorSet[]
            {
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = shader.GlobalDescriptorSets[imageIndex],
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                },
            };

            fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites)
            {
                context.Vk.UpdateDescriptorSets(context.Device.LogicalDevice, (uint)descriptorWrites.Length, descriptorWritesPtr, 0, default);
            }
            

            //bind the global descriptor set to be updated
            context.Vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, shader.Pipeline.PipelineLayout, 0, 1, globalDescriptor, 0, default);
        }

        public void SetModel(VulkanContext context, VulkanMaterialShader shader, Matrix4X4<float> model)
        {
            var imageIndex = context.ImageIndex;
            CommandBuffer commandBuffer = context.GraphicsCommandBuffers![imageIndex].Handle;

            // Used to push data that changes often that is usaged in all the shaders.
            // I think I can use this for time of day later
            // 128 bytes limited. Maybe not then for time of day
            context.Vk.CmdPushConstants<Matrix4X4<float>>(commandBuffer, shader.Pipeline.PipelineLayout, ShaderStageFlags.VertexBit, 0, (uint)sizeof(Matrix4X4<float>), ref model);  
        }

        public void ApplyMaterial(VulkanContext context, VulkanMaterialShader shader, Material material)
        {
            var imageIndex = context.ImageIndex;
            CommandBuffer commandBuffer = context.GraphicsCommandBuffers![imageIndex].Handle;

            //obtain material data
            var instanceState = shader.InstanceStates[material.InternalId];
            var instanceDescriptorSet = instanceState.DescriptorSets[imageIndex];

            //TODO: if needs update
            var descriptorWrites = new WriteDescriptorSet[VulkanMaterialShader.DESCRIPTOR_COUNT];

            // Descriptor 0 - Uniform buffer
            uint range = (uint)sizeof(VulkanMaterialShaderInstanceUniformObject);
            ulong offset = range * (ulong)material.InternalId; // also the index into the array

            // Get diffuse color from a material
            var instanceUbo = new VulkanMaterialShaderInstanceUniformObject()
            {
                DiffuseColor = material.DiffuseColor,
            };

            Span<VulkanMaterialShaderInstanceUniformObject> oboData = stackalloc VulkanMaterialShaderInstanceUniformObject[1];
            oboData[0] = instanceUbo;
            //load the data into the buffer
            _bufferSetup.BufferLoadData(context, shader.ObjectUniformBuffer, offset, range, 0, oboData);

            uint descriptorCount = 0; //count for writes
            uint descriptorIndex = 0;
            //only do this if the descriptor has not yet been updated
            var globalUboGeneration = instanceState.DescriptorStates[descriptorIndex].Generation[imageIndex];
            if (globalUboGeneration == EntityIdService.INVALID_ID || globalUboGeneration != material.Generation)
            {

                DescriptorBufferInfo bufferInfo = new()
                {
                    Buffer = shader.ObjectUniformBuffer.Handle,
                    Offset = offset,
                    Range = range,
                };

                var descriptor = new WriteDescriptorSet()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = instanceDescriptorSet,
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                };
                descriptorWrites[descriptorCount] = descriptor;
                descriptorCount++;

                //update the frame generation. In this case it is only needed once since this is a buffer.
                globalUboGeneration = material.Generation;
                instanceState.DescriptorStates[descriptorIndex].Generation[imageIndex] = globalUboGeneration;
            }

            descriptorIndex++;

            // Samplers.
            const uint samplerCount = 1;
            var imageInfos = new DescriptorImageInfo[1];
            for (uint samplerIndex = 0; samplerIndex < samplerCount; ++samplerIndex)
            {
                var textureUse = shader.SamplerUses[samplerIndex];
                Texture texture;
                switch (textureUse)
                {
                    case TextureUse.MapDiffuse:
                        texture = material.DiffuseMap.Texture;
                        break;
                    default:
                        _logger.LogError("Unable to bind sampler to unknown use.");
                        return;

                }

                var descriptorGeneration = instanceState.DescriptorStates[descriptorIndex].Generation[imageIndex];
                var descriptorId = instanceState.DescriptorStates[descriptorIndex].Ids[imageIndex];

                // if the texture hasn't been loaded yet, use the default.
                // TODO: Determine which use the texture has and pull appropriate default based on that.
                if (texture.Generation == EntityIdService.INVALID_ID)
                {
                    texture = _textureSystem.GetDefaultTexture();

                    //reset the descriptor generation if using the default texture.
                    descriptorGeneration = EntityIdService.INVALID_ID;
                }

                // Check if the descriptor needs updating first.
                if (descriptorId != texture.Id || descriptorGeneration != texture.Generation || descriptorGeneration == EntityIdService.INVALID_ID)
                {

                    if (texture.InternalData is not VulkanTextureData)
                    {
                        throw new EngineException($"Vulkan Image data not loaded for {texture.Name}!");
                    }
                    var internal_data = (VulkanTextureData)texture.InternalData;

                    // Assign view and sampler.
                    imageInfos[samplerIndex].ImageLayout = ImageLayout.ShaderReadOnlyOptimal;
                    imageInfos[samplerIndex].ImageView = internal_data.Image.ImageView;
                    imageInfos[samplerIndex].Sampler = internal_data.Sampler;

                    fixed (DescriptorImageInfo* imageInfosPtr = imageInfos)
                    {
                        var descriptor = new WriteDescriptorSet()
                        {
                            SType = StructureType.WriteDescriptorSet,
                            DstSet = instanceDescriptorSet,
                            DstBinding = descriptorIndex,
                            DstArrayElement = 0,
                            DescriptorType = DescriptorType.CombinedImageSampler,
                            DescriptorCount = 1,
                            PImageInfo = imageInfosPtr,
                        };
                        descriptorWrites[descriptorCount] = descriptor;
                        descriptorCount++;
                    }

                    // Sync frame generation if not using a default texture.
                    if (texture.Generation != EntityIdService.INVALID_ID)
                    {
                        descriptorGeneration = texture.Generation;
                        descriptorId = texture.Id;
                    }
                    //update the frame generation.
                    instanceState.DescriptorStates[descriptorIndex].Generation[imageIndex] = descriptorGeneration;
                    instanceState.DescriptorStates[descriptorIndex].Ids[imageIndex] = descriptorId;
                    descriptorIndex++;
                }
            }

            if (descriptorCount > 0)
            {
                fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites)
                {
                    context.Vk.UpdateDescriptorSets(context.Device.LogicalDevice, descriptorCount, descriptorWritesPtr, 0, default);
                }
            }

            //bind the descriptor set to be updated, or in case the shader changed.
            context.Vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, shader.Pipeline.PipelineLayout, 1, 1, instanceDescriptorSet, 0, default);

            //set data
            instanceState.DescriptorSets[imageIndex] = instanceDescriptorSet;
            shader.InstanceStates[material.InternalId] = instanceState;
        }

        public void AcquireResources(VulkanContext context, VulkanMaterialShader shader, Material material)
        {
            var internalId = shader.ObjectUniformBufferIndex;
            shader.ObjectUniformBufferIndex++;
            context.SetupBuiltinMaterialShader(shader);

            var instanceState = shader.InstanceStates[internalId];
            for(int cnt = 0; cnt < instanceState.DescriptorStates.Length; cnt++)
            {
                //set for all frames
                instanceState.DescriptorStates[cnt].Generation = new uint[3];
                Array.Fill(instanceState.DescriptorStates[cnt].Generation, EntityIdService.INVALID_ID);
                instanceState.DescriptorStates[cnt].Ids = new uint[3];
                Array.Fill(instanceState.DescriptorStates[cnt].Ids, EntityIdService.INVALID_ID);
            }

            var layouts = new DescriptorSetLayout[context.Swapchain!.SwapchainImages!.Length];
            for (int cnt = 0; cnt < layouts.Length; cnt++)
            {
                layouts[cnt] = shader.ObjectDescriptorSetLayout;
            }

            //global descriptor sets
            fixed (DescriptorSetLayout* layoutsPtr = layouts)
            {
                DescriptorSetAllocateInfo allocateInfo = new()
                {
                    SType = StructureType.DescriptorSetAllocateInfo,
                    DescriptorPool = shader.ObjectDescriptorPool,
                    DescriptorSetCount = (uint)layouts.Length,
                    PSetLayouts = layoutsPtr,
                };

                fixed (DescriptorSet* descriptorSetsPtr = instanceState.DescriptorSets)
                {
                    var allocDescriptorSetsResult = context.Vk.AllocateDescriptorSets(context.Device.LogicalDevice, allocateInfo, descriptorSetsPtr);
                    if (allocDescriptorSetsResult != Result.Success)
                    {
                        throw new VulkanResultException(allocDescriptorSetsResult, "Failed to allocate descriptor sets in shader!");
                    }
                }
            }
            //don't forget to updat the struct
            shader.InstanceStates[internalId] = instanceState;

            material.UpdateInternalId(internalId);

            context.SetupBuiltinMaterialShader(shader);
        }

        public void ReleaseResources(VulkanContext context, VulkanMaterialShader shader, Material material)
        {
            if(material.InternalId == EntityIdService.INVALID_ID)
            {
                return;
            }

            //Wait for any pending operations using the descriptor set to finish.
            context.Vk.DeviceWaitIdle(context.Device.LogicalDevice);

            var instanceState = shader.InstanceStates[material.InternalId];

            var result = context.Vk.FreeDescriptorSets(context.Device.LogicalDevice, shader.ObjectDescriptorPool, (uint)instanceState.DescriptorSets.Length, instanceState.DescriptorSets);
            if(result != Result.Success)
            {
                _logger.LogError("Error freeing material shader descriptor sets!");
            }
            //reset sets
            Array.Fill(instanceState.DescriptorSets, default);

            for (int cnt = 0; cnt < instanceState.DescriptorStates.Length; cnt++)
            {
                //set for all frames
                Array.Fill(instanceState.DescriptorStates[cnt].Generation, EntityIdService.INVALID_ID);
                Array.Fill(instanceState.DescriptorStates[cnt].Ids, EntityIdService.INVALID_ID);
            }
            shader.InstanceStates[material.InternalId] = instanceState;

            //TODO: add objectId to free list

            material.UpdateInternalId(EntityIdService.INVALID_ID);

            context.SetupBuiltinMaterialShader(shader);

        }
    }
}
