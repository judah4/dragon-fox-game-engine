using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanContext
    {
        public Vk Vk { get;}

        public IWindow Window { get; }

        public Instance Instance { get; }
        public AllocationCallbacks* Allocator { get; }

        public VulkanContext(Vk vk, IWindow window, Instance instance, AllocationCallbacks* allocator)
        {
            Vk = vk;
            Window = window;
            Instance = instance;
            Allocator = allocator;
        }

    }
}
