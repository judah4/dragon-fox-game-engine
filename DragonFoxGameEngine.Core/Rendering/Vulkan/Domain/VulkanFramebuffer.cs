using Silk.NET.Vulkan;
using System;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct VulkanFramebuffer
    {
        public Framebuffer Framebuffer;
        public ImageView[] Attachments;
        public VulkanRenderpass Renderpass;
    }
}
