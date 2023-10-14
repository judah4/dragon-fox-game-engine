using DragonGameEngine.Core.Rendering.Vulkan;
using Silk.NET.Vulkan;
using System;

namespace DragonGameEngine.Core.Exceptions.Vulkan
{
    public class VulkanResultException : Exception
    {
        public Result Result { get; }

        public VulkanResultException(Result result, string message)
            : base($"{message} Result:{result} {VulkanUtils.FormattedResult(result)}") 
        {
            Result = result;
        }
    }
}
