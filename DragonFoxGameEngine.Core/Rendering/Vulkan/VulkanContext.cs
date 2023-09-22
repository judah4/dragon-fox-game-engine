using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanContext
    {
        public Vk Vk { get;}

        public IWindow Window { get; }

        public Instance Instance { get; }
        public AllocationCallbacks* Allocator { get; }

        //debugging
        public ExtDebugUtils? DebugUtils { get; }
        public DebugUtilsMessengerEXT DebugMessenger { get;}

        public KhrSurface? KhrSurface { get; private set; }
        public SurfaceKHR? Surface { get; private set; }

        public VulkanDevice Device { get; private set; }

        public VulkanContext(
            Vk vk, 
            IWindow window, 
            Instance instance, 
            AllocationCallbacks* allocator, 
            ExtDebugUtils? debugUtils, 
            DebugUtilsMessengerEXT debugMessenger)
        {
            Vk = vk;
            Window = window;
            Instance = instance;
            Allocator = allocator;
            DebugUtils = debugUtils;
            DebugMessenger = debugMessenger;
        }

        /// <summary>
        /// For Surface setup
        /// </summary>
        /// <param name="khrSurface"></param>
        /// <param name="surface"></param>
        public void SetupSurface(KhrSurface khrSurface, SurfaceKHR surface)
        {
            KhrSurface = khrSurface;
            Surface = surface;
        }

        /// <summary>
        /// For Device setup
        /// </summary>
        /// <param name="khrSurface"></param>
        /// <param name="surface"></param>
        public void SetupDevice(VulkanDevice device)
        {
            Device = device;
        }
    }
}
