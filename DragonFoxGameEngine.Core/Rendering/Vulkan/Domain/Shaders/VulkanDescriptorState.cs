namespace DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    public struct VulkanDescriptorState
    {
        //per frame
        public uint[] Generation { get; set; }
        public uint[] Ids { get; set; }
    }
}
