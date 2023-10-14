using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Exceptions.Vulkan;
using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders;
using DragonGameEngine.Core.Resources;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System;

namespace DragonGameEngine.Core.Rendering.Vulkan.Shaders
{
    public unsafe class VulkanObjectShaderSetup
    {
        public const string BUILTIN_SHADER_NAME_OBJECT = "Builtin.ObjectShader";

        private readonly ILogger _logger;
        private readonly VulkanShaderSetup _shaderSetup;
        private readonly VulkanPipelineSetup _pipelineSetup;
        private readonly VulkanBufferSetup _bufferSetup;

        private float tempAccumulator = 0f;

        public VulkanObjectShaderSetup(ILogger logger, VulkanShaderSetup shaderSetup, VulkanPipelineSetup pipelineSetup, VulkanBufferSetup bufferSetup)
        {
            _logger = logger;
            _shaderSetup = shaderSetup;
            _pipelineSetup = pipelineSetup;
            _bufferSetup = bufferSetup;
        }

        public VulkanObjectShader Create(VulkanContext context, Texture defaultDiffuse)
        {
            // Shader module init per stage
            var stageTypesNames = new string[VulkanObjectShader.OBJECT_SHADER_STAGE_COUNT] { "vert", "frag" };
            var stageTypes = new ShaderStageFlags[VulkanObjectShader.OBJECT_SHADER_STAGE_COUNT] { ShaderStageFlags.VertexBit, ShaderStageFlags.FragmentBit };

            var objectShader = new VulkanObjectShader() 
            {
                DefaultDiffuse = defaultDiffuse,
            };
            for (int cnt = 0; cnt < VulkanObjectShader.OBJECT_SHADER_STAGE_COUNT; cnt++)
            {
                var shaderModuleResult = _shaderSetup.CreateShaderModule(context, BUILTIN_SHADER_NAME_OBJECT, stageTypesNames[cnt], stageTypes[cnt], cnt);
                objectShader.ShaderStages[cnt] = shaderModuleResult;
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

                var createDescriptorPoolResult = context.Vk.CreateDescriptorPool(context.Device.LogicalDevice, poolInfo, context.Allocator, &descriptorPool);
                if (createDescriptorPoolResult != Result.Success)
                {
                    throw new VulkanResultException(createDescriptorPoolResult, "Failed to create descriptor pool!");
                }

                objectShader.GlobalDescriptorPool = descriptorPool;
            }

            //Local/Object Descriptors
            const uint localSamplerCount = 1;
            var descriptorTypes = new DescriptorType[VulkanObjectShaderObjectState.DESCRIPTOR_COUNT] 
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
                objectShader.ObjectDescriptorSetLayout = localDescriptorSetLayout;
            }

            var localPoolSizes = new DescriptorPoolSize[]
            {
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.UniformBuffer,
                    DescriptorCount = VulkanObjectShader.MAX_OBJECT_COUNT,
                },
                new DescriptorPoolSize()
                {
                    Type = DescriptorType.CombinedImageSampler,
                    DescriptorCount = localSamplerCount * VulkanObjectShader.MAX_OBJECT_COUNT,
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
                    MaxSets = VulkanObjectShader.MAX_OBJECT_COUNT,
                };

                var createDescriptorPoolResult = context.Vk.CreateDescriptorPool(context.Device.LogicalDevice, poolInfo, context.Allocator, &localDescriptorPool);
                if (createDescriptorPoolResult != Result.Success)
                {
                    throw new VulkanResultException(createDescriptorPoolResult, "Failed to create local/object descriptor pool!");
                }

                objectShader.ObjectDescriptorPool = localDescriptorPool;
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
            var stages = new PipelineShaderStageCreateInfo[objectShader.ShaderStages.Length];
            for (int cnt = 0; cnt < objectShader.ShaderStages.Length; cnt++)
            {
                stages[cnt] = objectShader.ShaderStages[cnt].ShaderStageCreateInfo;
            }

            var vulkanPipeline = _pipelineSetup.PipelineCreate(context, context.MainRenderPass, attributeDescriptions, descriptorSetLayouts, stages, viewport, scissor, false);

            objectShader.Pipeline = vulkanPipeline;

            //create global uniform buffer
            MemoryPropertyFlags deviceLocalBits = context.Device.SupportsDeviceLocalHostVisible ? MemoryPropertyFlags.DeviceLocalBit : MemoryPropertyFlags.None;
            var globalUboBuffer = _bufferSetup.BufferCreate(
                context,
                (ulong)(sizeof(GlobalUniformObject) * 3), //make this large enough for each frame
                BufferUsageFlags.TransferDstBit | BufferUsageFlags.UniformBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit | deviceLocalBits,
                true);
            objectShader.GlobalUniformBuffer = globalUboBuffer;

            var globalLayouts = new DescriptorSetLayout[3];
            for (int cnt = 0; cnt < globalLayouts.Length; cnt++)
            {
                globalLayouts[cnt] = objectShader.GlobalDescriptorSetLayout;
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

                fixed (DescriptorSet* descriptorSetsPtr = objectShader.GlobalDescriptorSets)
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
                (ulong)sizeof(ObjectUniformObject),
                BufferUsageFlags.TransferDstBit | BufferUsageFlags.UniformBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                true);
            objectShader.ObjectUniformBuffer = localUboBuffer;

            return objectShader;
        }

        public VulkanObjectShader Destroy(VulkanContext context, VulkanObjectShader shader)
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

        public void ShaderUse(VulkanContext context, VulkanObjectShader shader)
        {
            var imageIndex = context.ImageIndex;
            _pipelineSetup.PipelineBind(context, context.GraphicsCommandBuffers![imageIndex], PipelineBindPoint.Graphics, shader.Pipeline);
        }

        public void UpdateGlobalState(VulkanContext context, VulkanObjectShader shader, double deltaTime)
        {
            var imageIndex = context.ImageIndex;
            CommandBuffer commandBuffer = context.GraphicsCommandBuffers![imageIndex].Handle;
            var globalDescriptor = shader.GlobalDescriptorSets[imageIndex];

            if (!shader.DescriptorUpdated[imageIndex])
            {
                //configure
                uint range = (uint)sizeof(GlobalUniformObject);
                ulong offset = range * imageIndex;

                Span<GlobalUniformObject> uboData = stackalloc GlobalUniformObject[1];
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
                shader.DescriptorUpdated[imageIndex]= true;
            }

            //bind the global descriptor set to be updated
            context.Vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, shader.Pipeline.PipelineLayout, 0, 1, globalDescriptor, 0, default);
        }

        public void UpdateObject(VulkanContext context, GeometryRenderData data)
        {
            var imageIndex = context.ImageIndex;
            CommandBuffer commandBuffer = context.GraphicsCommandBuffers![imageIndex].Handle;
            var shader = context.ObjectShader!;

            // Used to push data that changes often that is usaged in all the shaders.
            // I think I can use this for time of day later
            // 128 bytes limited. Maybe not then for time of day
            context.Vk.CmdPushConstants<Matrix4X4<float>>(commandBuffer, shader.Pipeline.PipelineLayout, ShaderStageFlags.VertexBit, 0, (uint)sizeof(Matrix4X4<float>), ref data.Model);
        
            //obtain material data
            var objectState = shader.ObjectStates[data.ObjectId];
            var objectDescriptorSet = objectState.DescriptorSets[imageIndex];

            //TODO: if needs update
            var descriptorWrites = new WriteDescriptorSet[VulkanObjectShaderObjectState.DESCRIPTOR_COUNT];

            // Descriptor 0 - Uniform buffer
            uint range = (uint)sizeof(ObjectUniformObject);
            ulong offset = range * (ulong)data.ObjectId; // also the index into the array

            //todo: get diffuse color from a material
            tempAccumulator += (float)context.FrameDeltaTime;
            var sinVal = (float)(Math.Sin(tempAccumulator) + 1f) / 2f; //Scale from -1, 1 to 0, 1
            var obo = new ObjectUniformObject()
            {
                DiffuseColor = new Vector4D<float>(sinVal, sinVal, sinVal, 1f),
            };

            Span<ObjectUniformObject> oboData = stackalloc ObjectUniformObject[1];
            oboData[0] = obo;
            //load the data into the buffer
            _bufferSetup.BufferLoadData(context, shader.ObjectUniformBuffer, offset, range, 0, oboData);

            uint descriptorCount = 0; //count for writes
            uint descriptorIndex = 0;
            //only do this if the descriptor has not yet been updated
            if (objectState.DescriptorStates[descriptorIndex].Generation[imageIndex] == EntityIdService.INVALID_ID)
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
                    DstSet = objectDescriptorSet,
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                };
                descriptorWrites[descriptorCount] = descriptor;
                descriptorCount++;

                //update the frame generation. In this case it is only needed once since this is a buffer.
                objectState.DescriptorStates[descriptorIndex].Generation[imageIndex] = 1;

            }

            descriptorIndex++;

            // TODO: samplers.
            const uint samplerCount = 1;
            var imageInfos = new DescriptorImageInfo[1];
            for (uint samplerIndex = 0; samplerIndex < samplerCount; ++samplerIndex)
            {
                if(data.Textures.Length <= samplerIndex)
                {
                    continue;
                }    
                var t = data.Textures[samplerIndex];
                var descriptorGeneration = objectState.DescriptorStates[descriptorIndex].Generation[imageIndex];

                // if the texture hasn't been loaded yet, use the default.
                // TODO: Determine which use the texture has and pull appropriate default based on that.
                if (t.Generation == EntityIdService.INVALID_ID)
                {
                    t = shader.DefaultDiffuse;

                    //reset the descriptor generation if using the default texture.
                    descriptorGeneration = EntityIdService.INVALID_ID;
                }

                // Check if the descriptor needs updating first.
                if (descriptorGeneration != t.Generation || descriptorGeneration == EntityIdService.INVALID_ID)
                {
                    var internal_data = (VulkanTextureData)t.Data.InternalData;

                    // Assign view and sampler.
                    imageInfos[samplerIndex].ImageLayout = ImageLayout.ShaderReadOnlyOptimal;
                    imageInfos[samplerIndex].ImageView = internal_data.Image.ImageView;
                    imageInfos[samplerIndex].Sampler = internal_data.Sampler;

                    fixed(DescriptorImageInfo* imageInfosPtr = imageInfos)
                    {
                        var descriptor = new WriteDescriptorSet()
                        {
                            SType = StructureType.WriteDescriptorSet,
                            DstSet = objectDescriptorSet,
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
                    if (t.Generation != EntityIdService.INVALID_ID)
                    {
                        descriptorGeneration = t.Generation;
                    }
                    //update the frame generation.
                    objectState.DescriptorStates[descriptorIndex].Generation[imageIndex] = descriptorGeneration;
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
            context.Vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, shader.Pipeline.PipelineLayout, 1, 1, objectDescriptorSet, 0, default);

            //set data
            objectState.DescriptorSets[imageIndex] = objectDescriptorSet;
            shader.ObjectStates[data.ObjectId] = objectState;
        }

        public uint AcquireResources(VulkanContext context)
        {
            var shader = context.ObjectShader;
            var objectId = shader.ObjectUniformBufferIndex;
            shader.ObjectUniformBufferIndex++;
            context.SetupBuiltinShaders(shader);

            var objectState = shader.ObjectStates[objectId];
            for(int cnt = 0; cnt < objectState.DescriptorStates.Length; cnt++)
            {
                //set for all frames
                objectState.DescriptorStates[cnt].Generation = new uint[3];
                Array.Fill(objectState.DescriptorStates[cnt].Generation, EntityIdService.INVALID_ID);
            }
            shader.ObjectStates[objectId] = objectState;

            var layouts = new DescriptorSetLayout[3];
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

                fixed (DescriptorSet* descriptorSetsPtr = objectState.DescriptorSets)
                {
                    var allocDescriptorSetsResult = context.Vk.AllocateDescriptorSets(context.Device.LogicalDevice, allocateInfo, descriptorSetsPtr);
                    if (allocDescriptorSetsResult != Result.Success)
                    {
                        throw new VulkanResultException(allocDescriptorSetsResult, "Failed to allocate descriptor sets in shader!");
                    }
                }
            }

            context.SetupBuiltinShaders(shader);

            return objectId;
        }
        public void ReleaseResources(VulkanContext context, uint objectId)
        {
            var shader = context.ObjectShader;
            var objectState = shader.ObjectStates[objectId];

            var result = context.Vk.FreeDescriptorSets(context.Device.LogicalDevice, shader.ObjectDescriptorPool, (uint)objectState.DescriptorSets.Length, objectState.DescriptorSets);
            if(result != Result.Success)
            {
                _logger.LogError("Error freeing object shader descriptor sets!");
            }

            for (int cnt = 0; cnt < objectState.DescriptorStates.Length; cnt++)
            {
                //set for all frames
                Array.Fill(objectState.DescriptorStates[cnt].Generation, EntityIdService.INVALID_ID);
            }
            shader.ObjectStates[objectId] = objectState;

            //TODO: add objectId to free list

            context.SetupBuiltinShaders(shader);

        }
    }
}
