using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain.Shaders;
using Foxis.Library;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using System;
using Result = Foxis.Library.Result;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Shaders
{
    public class VulkanObjectShaderSetup
    {
        public const string BUILTIN_SHADER_NAME_OBJECT = "Builtin.ObjectShader";

        private readonly ILogger _logger;
        private readonly VulkanShaderSetup _shaderSetup;

        public VulkanObjectShaderSetup(ILogger logger, VulkanShaderSetup shaderSetup)
        {
            _logger = logger;
            _shaderSetup = shaderSetup;
        }

        public Result<VulkanObjectShader> ObjectShaderCreate(VulkanContext context)
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
                    return Result.Fail<VulkanObjectShader>("Failed to create");
                }
                objectShader.ShaderStages[cnt] = shaderModuleResult.Value;
            }

            //Descriptors
            //are next

            return Result.Ok(objectShader);
        }

        public VulkanObjectShader ObjectShaderDestroy(VulkanContext context, VulkanObjectShader shader)
        {
            throw new NotImplementedException();
        }

        public VulkanObjectShader ObjectShaderUse(VulkanContext context, VulkanObjectShader shader)
        {
            throw new NotImplementedException();
        }


    }
}
