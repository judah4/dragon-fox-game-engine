﻿using DragonGameEngine.Core.Exceptions.Vulkan;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using System.IO;

namespace DragonGameEngine.Core.Rendering.Vulkan.Shaders
{
    public unsafe sealed class VulkanShaderManager
    {

        private readonly ResourceSystem _resourceSystem;
        public VulkanShaderManager(ResourceSystem resourceSystem) 
        {
            _resourceSystem = resourceSystem;
        }

        public VulkanShaderStage CreateShaderModule(VulkanContext context, string shaderName, string stageTypeName, ShaderStageFlags shaderStageFlags, int index)
        {

            //Build file name.
            var fileName = Path.Combine("Shaders/", $"{shaderName}.{stageTypeName}.spv");

            var resource = _resourceSystem.Load(fileName, ResourceType.Binary);

            var shaderCode = (byte[])resource.Data;

            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)shaderCode.Length,
            };

            ShaderModule shaderModule;

            fixed (byte* codePtr = shaderCode)
            {
                createInfo.PCode = (uint*)codePtr;

                var createShaderResult = context.Vk.CreateShaderModule(context.Device.LogicalDevice, createInfo, context.Allocator, out shaderModule);
                if (createShaderResult != Silk.NET.Vulkan.Result.Success)
                {
                    throw new VulkanResultException(createShaderResult, $"Unable to create {stageTypeName} shader module for {shaderName}");
                }
            }

            //Shader stage info

            PipelineShaderStageCreateInfo shaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = shaderStageFlags,
                Module = shaderModule,
                PName = (byte*)SilkMarshal.StringToPtr("main")
            };

            var shaderStage = new VulkanShaderStage()
            {
                CreateInfo = createInfo,
                Handle = shaderModule,
                ShaderStageCreateInfo = shaderStageInfo,
            };

            _resourceSystem.Unload(resource);

            return shaderStage;
        }
    }
}
