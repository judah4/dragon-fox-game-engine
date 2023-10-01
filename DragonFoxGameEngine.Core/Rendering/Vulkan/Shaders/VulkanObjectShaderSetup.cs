using DragonFoxGameEngine.Core.Maths;
using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain;
using DragonFoxGameEngine.Core.Rendering.Vulkan.Domain.Shaders;
using Foxis.Library;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using System;
using Result = Foxis.Library.Result;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Shaders
{
    public unsafe class VulkanObjectShaderSetup
    {
        public const string BUILTIN_SHADER_NAME_OBJECT = "Builtin.ObjectShader";

        private readonly ILogger _logger;
        private readonly VulkanShaderSetup _shaderSetup;
        private readonly VulkanPipelineSetup _pipelineSetup;

        public VulkanObjectShaderSetup(ILogger logger, VulkanShaderSetup shaderSetup, VulkanPipelineSetup pipelineSetup)
        {
            _logger = logger;
            _shaderSetup = shaderSetup;
            _pipelineSetup = pipelineSetup;
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
            //TODO: DESCRIPTORS
            var descriptors = new DescriptorSetLayout[0];

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

            //Stages
            //Note: should match the number of shader.stages
            var stages = new PipelineShaderStageCreateInfo[objectShader.ShaderStages.Length];
            for(int cnt = 0; cnt < objectShader.ShaderStages.Length; cnt++)
            {
                stages[cnt] = objectShader.ShaderStages[cnt].ShaderStageCreateInfo;
            }

            var vulkanPipeline = _pipelineSetup.PipelineCreate(context, context.MainRenderPass, attributeDescriptions, descriptors, stages, viewport, scissor, false);

            objectShader.Pipeline = vulkanPipeline;

            return Result.Ok(objectShader);
        }

        public VulkanObjectShader ObjectShaderDestroy(VulkanContext context, VulkanObjectShader shader)
        {

            _pipelineSetup.PipelineDestroy(context, shader.Pipeline);

            //destroy shader modules
            for(int cnt = 0; cnt <  shader.ShaderStages.Length; cnt++)
            {
                context.Vk.DestroyShaderModule(context.Device.LogicalDevice, shader.ShaderStages[cnt].Handle, context.Allocator);
                shader.ShaderStages[cnt].Handle = default;
            }
            return shader;
        }

        public VulkanObjectShader ObjectShaderUse(VulkanContext context, VulkanObjectShader shader)
        {
            throw new NotImplementedException();
        }

    }
}
