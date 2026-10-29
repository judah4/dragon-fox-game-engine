using Silk.NET.Vulkan;
using System;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct VulkanFramebuffer
    {
        public Framebuffer Framebuffer;
        public ImageView[] Attachments;
        public VulkanRenderpass Renderpass;
    }
}
