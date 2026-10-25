using System;
using System.Collections.Generic;
using DragonGameEngine.Core.Exceptions;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    public sealed class VulkanDescriptorState
    {
        // per frame values
        private readonly DescriptorGeneration[] _generations;
        
        public IReadOnlyList<DescriptorGeneration> Generations => _generations;
        
        public VulkanDescriptorState(int capacity, DescriptorGeneration defaultValue)
        {
            _generations = new DescriptorGeneration[capacity];
            Array.Fill(_generations, defaultValue);
        }

        /// <summary>
        /// Update the current state of the descriptor generation and id
        /// </summary>
        /// <param name="imageIndex"></param>
        /// <param name="generation"></param>
        /// <exception cref="EngineException"></exception>
        public void Update(int imageIndex, DescriptorGeneration generation)
        {
            if (imageIndex >= _generations.Length)
            {
                throw new EngineException("Generations index out of range. Check the number of swapchains and framebuffers.");
            }
            _generations[imageIndex] = generation;
        }
    }

    public readonly struct DescriptorGeneration
    {
        public uint Generation { get; }
        public uint DescriptorId { get; }
        
        public DescriptorGeneration(uint generation, uint descriptorId)
        {
            Generation = generation;
            DescriptorId = descriptorId;
        }
    }
}
