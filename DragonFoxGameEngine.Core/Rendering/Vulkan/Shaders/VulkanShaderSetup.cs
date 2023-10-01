using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain.Shaders;
using Foxis.Library;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using System;
using System.IO;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Shaders
{
    public unsafe class VulkanShaderSetup
    {
        public Result<VulkanShaderStage> CreateShaderModule(VulkanContext context, string shaderName, string stageTypeName, ShaderStageFlags shaderStageFlags, int index)
        {
            //Build file name.
            var fileName = Path.Combine("Shaders/", $"{shaderName}.{stageTypeName}.spv");

            var shaderCode = File.ReadAllBytes(fileName);

            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)shaderCode.Length,
            };

            ShaderModule shaderModule;

            fixed (byte* codePtr = shaderCode)
            {
                createInfo.PCode = (uint*)codePtr;

                if (context.Vk.CreateShaderModule(context.Device.LogicalDevice, createInfo, context.Allocator, out shaderModule) != Silk.NET.Vulkan.Result.Success)
                {
                    throw new Exception("Shader was not able to be created!");
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

            return Foxis.Library.Result.Ok(shaderStage);
        }
    }
}
