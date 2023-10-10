using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System;

namespace DragonGameEngine.Core.Rendering.Vulkan
{
    public unsafe class VulkanImageSetup
    {
        private readonly ILogger _logger;

        public VulkanImageSetup(ILogger logger)
        {
            _logger = logger;
        }

        public VulkanImage ImageCreate(VulkanContext context, ImageType imageType, Vector2D<uint> size, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags memoryFlags,
            bool createView, ImageAspectFlags viewAspectFlags)
        {
            var vulkanImage = new VulkanImage()
            {
                Size = size,
            };

            ImageCreateInfo imageCreateInfo = new()
            {
                SType = StructureType.ImageCreateInfo,
                ImageType = ImageType.Type2D,
                Extent =
                {
                    Width = size.X,
                    Height = size.Y,
                    Depth = 1, // TODO: support configurable depth
                },
                MipLevels = 4, // TODO: support mip mapping
                ArrayLayers = 1, //TODO: support number of layers in the image
                Format = format,
                Tiling = tiling,
                InitialLayout = ImageLayout.Undefined,
                Usage = usage,
                Samples = SampleCountFlags.Count1Bit, //TODO: configurable sample count
                SharingMode = SharingMode.Exclusive, //TODO: configurable sharing mode.
            };

            if (context.Vk.CreateImage(context.Device.LogicalDevice, imageCreateInfo, context.Allocator, &vulkanImage.Handle) != Result.Success)
            {
                throw new Exception("Failed to create image!");
            }

            context.Vk.GetImageMemoryRequirements(context.Device.LogicalDevice, vulkanImage.Handle, out MemoryRequirements memRequirements);

            //query memory requirements
            var memoryType = FindMemoryIndex(context, memRequirements.MemoryTypeBits, memoryFlags);

            //allocate the memory
            MemoryAllocateInfo allocInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = memoryType,
            };

            if (context.Vk.AllocateMemory(context.Device.LogicalDevice, allocInfo, context.Allocator, &vulkanImage.Memory) != Result.Success)
            {
                throw new Exception("Failed to allocate image memory!");
            }

            //bind the memory
            context.Vk.BindImageMemory(context.Device.LogicalDevice, vulkanImage.Handle, vulkanImage.Memory, 0); //TODO: configurable memory offset

            if (createView)
            {
                vulkanImage.ImageView = default;
                vulkanImage = ImageViewCreate(context, format, vulkanImage, viewAspectFlags);
            }

            _logger.LogDebug("Image is created!");

            return vulkanImage;
        }

        public VulkanImage ImageViewCreate(VulkanContext context, Format format, VulkanImage vulkanImage, ImageAspectFlags viewAspectFlags)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = vulkanImage.Handle,
                ViewType = ImageViewType.Type2D,
                Format = format,
                SubresourceRange = //TODO: make configurable
                    {
                        AspectMask = viewAspectFlags,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                    }
            };

            if (context.Vk!.CreateImageView(context.Device.LogicalDevice, createInfo, context.Allocator, out ImageView imageView) != Result.Success)
            {
                throw new Exception("Failed to create image views!");
            }
            vulkanImage.ImageView = imageView;
            return vulkanImage;
        }

        public VulkanImage ImageDestroy(VulkanContext context, VulkanImage vulkanImage)
        {
            context.Vk.DestroyImageView(context.Device.LogicalDevice, vulkanImage.ImageView, null);
            context.Vk.FreeMemory(context.Device.LogicalDevice, vulkanImage.Memory, null);
            context.Vk.DestroyImage(context.Device.LogicalDevice, vulkanImage.Handle, null);

            vulkanImage.ImageView = default;
            vulkanImage.Memory = default;
            vulkanImage.Handle = default;
            _logger.LogDebug("Image is destroyed.");
            return vulkanImage;
        }

        public uint FindMemoryIndex(VulkanContext context, uint typeFilter, MemoryPropertyFlags properties)
        {
            context.Vk.GetPhysicalDeviceMemoryProperties(context.Device.PhysicalDevice, out PhysicalDeviceMemoryProperties memProperties);

            for (int i = 0; i < memProperties.MemoryTypeCount; i++)
            {
                if ((typeFilter & 1 << i) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                {
                    return (uint)i;
                }
            }

            throw new Exception("Failed to find suitable memory type!");
        }
    }
}
