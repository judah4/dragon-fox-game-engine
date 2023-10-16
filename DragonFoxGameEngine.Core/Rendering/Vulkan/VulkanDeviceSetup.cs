using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Exceptions.Vulkan;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DragonGameEngine.Core.Rendering.Vulkan
{
    /// <summary>
    /// Physical and Logical Device setup for Vulakn
    /// </summary>
    public unsafe class VulkanDeviceSetup
    {
        /// <summary>
        /// Device Requirements
        /// </summary>
        struct PhysicalDeviceRequirements
        {
            public bool Graphics;
            public bool Present;
            public bool Compute;
            public bool Transfer;
            public string[] DeviceExtensions = new[]
            {
                KhrSwapchain.ExtensionName
            };
            public bool SamplerAnisotropy;
            public bool DiscreteGpu;

            public PhysicalDeviceRequirements()
            {

            }
        }

        private readonly ILogger _logger;

        public VulkanDeviceSetup(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Select the Physical Device and create the Logical Device
        /// </summary>
        /// <param name="context"></param>
        public void Create(VulkanContext context)
        {
            //todo: make this engine configurable
            var requirements = new PhysicalDeviceRequirements()
            {
                Graphics = true,
                Present = true,
                Transfer = true,
                Compute = false,
                SamplerAnisotropy = true,
                DiscreteGpu = false,
            };

            SelectPhysicalDevice(context, requirements);

            bool presentSharesGraphicsQueue = context.Device.QueueFamilyIndices.GraphicsFamilyIndex == context.Device.QueueFamilyIndices.PresentFamilyIndex;
            bool transferSharesGraphicsQueue = context.Device.QueueFamilyIndices.GraphicsFamilyIndex == context.Device.QueueFamilyIndices.TransferFamilyIndex;
            int indexCount = 1;
            if (!presentSharesGraphicsQueue)
            {
                indexCount++;
            }
            if (!transferSharesGraphicsQueue)
            {
                indexCount++;
            }
            if (requirements.Compute)
            {
                indexCount++;
            }

            uint[] indices = new uint[indexCount];
            byte index = 0;
            indices[index++] = context.Device.QueueFamilyIndices.GraphicsFamilyIndex;
            if (!presentSharesGraphicsQueue)
            {
                indices[index++] = context.Device.QueueFamilyIndices.PresentFamilyIndex;
            }
            if (!transferSharesGraphicsQueue)
            {
                indices[index++] = context.Device.QueueFamilyIndices.TransferFamilyIndex;
            }
            if (requirements.Compute)
            {
                indices[index++] = context.Device.QueueFamilyIndices.ComputeFamilyIndex;
            }

            using var mem = GlobalMemory.Allocate(indexCount * sizeof(DeviceQueueCreateInfo));
            var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

            using var memQPrio = GlobalMemory.Allocate(1 * sizeof(float));
            var queuePriority = (float*)Unsafe.AsPointer(ref mem.GetPinnableReference());

            queuePriority[0] = 0.999f;
            for (int i = 0; i < indexCount; i++)
            {
                queueCreateInfos[i] = new()
                {
                    SType = StructureType.DeviceQueueCreateInfo,
                    QueueFamilyIndex = indices[i],
                    QueueCount = 1,
                    PQueuePriorities = queuePriority,
                };
                //TODO: Enable for future enhancement
                //if (indices[i] == context.Device.QueueFamilyIndices.GraphicsFamilyIndex)
                //{
                //    queueCreateInfos[i].QueueCount = 2; //more passes?
                //}
            }

            PhysicalDeviceFeatures deviceFeatures = new()
            {
                SamplerAnisotropy = requirements.SamplerAnisotropy,
            };

            DeviceCreateInfo createInfo = new()
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = (uint)indexCount,
                PQueueCreateInfos = queueCreateInfos,

                PEnabledFeatures = &deviceFeatures,

                EnabledExtensionCount = (uint)requirements.DeviceExtensions.Length,
                PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(requirements.DeviceExtensions),

                //layers are deprecated, do not use.
                EnabledLayerCount = 0,
            };

            var createDeviceResult = context.Vk.CreateDevice(context.Device.PhysicalDevice, in createInfo, context.Allocator, out var device);
            if (createDeviceResult != Result.Success)
            {
                throw new VulkanResultException(createDeviceResult, "Failed to create logical device!");
            }

            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

            var vDevice = context.Device;
            vDevice.LogicalDevice = device;

            _logger.LogDebug("Logical device created.");

            context.Vk.GetDeviceQueue(device, context.Device.QueueFamilyIndices.GraphicsFamilyIndex, 0, out vDevice.GraphicsQueue);
            context.Vk.GetDeviceQueue(device, context.Device.QueueFamilyIndices.PresentFamilyIndex, 0, out vDevice.PresentQueue);
            context.Vk.GetDeviceQueue(device, context.Device.QueueFamilyIndices.TransferFamilyIndex, 0, out vDevice.TransferQueue);
            if (requirements.Compute)
            {
                Queue computeQueue;
                context.Vk.GetDeviceQueue(device, context.Device.QueueFamilyIndices.ComputeFamilyIndex, 0, out computeQueue);
                vDevice.ComputeQueue = computeQueue;
            }

            //Create command pool for graphics queue.
            CommandPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = vDevice.QueueFamilyIndices.GraphicsFamilyIndex,
                Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            };

            var createCommandPoolResult = context.Vk.CreateCommandPool(vDevice.LogicalDevice, poolInfo, context.Allocator, out var commandPool);
            if (createCommandPoolResult != Result.Success)
            {
                throw new VulkanResultException(createCommandPoolResult, "Failed to create command pool!");
            }
            vDevice.GraphicsCommandPool = commandPool;

            context.SetupDevice(vDevice);
        }

        /// <summary>
        /// Destroy the Logical Device
        /// </summary>
        /// <param name="context"></param>
        public void Destroy(VulkanContext context)
        {
            context.Vk.DestroyCommandPool(context.Device.LogicalDevice, context.Device.GraphicsCommandPool, context.Allocator);

            context.Vk.DestroyDevice(context.Device.LogicalDevice, context.Allocator);

            context.SetupDevice(default); //clears out some arrays and stuff

            _logger.LogDebug("Logical device destroyed.");
        }

        /// <summary>
        /// Query swap chain support
        /// </summary>
        /// <param name="physicalDevice"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public VulkanSwapchainSupportInfo QuerySwapChainSupport(PhysicalDevice physicalDevice, VulkanContext context)
        {
            var surface = context.Surface!.Value;
            context.KhrSurface!.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out var capabilities);
            uint formatCount = 0;
            context.KhrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, null);

            var formats = Array.Empty<SurfaceFormatKHR>();;
            if (formatCount != 0)
            {
                formats = new SurfaceFormatKHR[formatCount];
                fixed (SurfaceFormatKHR* formatsPtr = formats)
                {
                    context.KhrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, formatsPtr);
                }
            }
            uint presentModeCount = 0;
            context.KhrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, null);

            var presentModes = Array.Empty<PresentModeKHR>();
            if (presentModeCount != 0)
            {
                presentModes = new PresentModeKHR[presentModeCount];
                fixed (PresentModeKHR* presentModesPtr = presentModes)
                {
                    context.KhrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, presentModesPtr);
                }
            }

            var details = new VulkanSwapchainSupportInfo()
            {
                Capabilities = capabilities,
                Formats = formats,
                PresentModes = presentModes,
            };

            return details;
        }

        /// <summary>
        /// Detect depth format and set it in device context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <remarks>Impure, sets device depth format in context.</remarks>
        public Format DetectDepthFormat(VulkanContext context)
        {
            var depthFormat = FindSupportedFormat(context, new[] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint }, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
            var device = context.Device;
            device.DepthFormat = depthFormat;
            context.SetupDevice(device);
            return depthFormat;
        }

        private Format FindSupportedFormat(VulkanContext context, IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
        {
            foreach (var format in candidates)
            {
                context.Vk!.GetPhysicalDeviceFormatProperties(context.Device.PhysicalDevice, format, out var props);

                if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
                {
                    return format;
                }
                else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
                {
                    return format;
                }
            }

            throw new EngineException($"Failed to find supported format! Tiling: {tiling} Features: {features}");
        }

        private void SelectPhysicalDevice(VulkanContext context, PhysicalDeviceRequirements requirements)
        {
            uint devicedCount = 0;
            context.Vk.EnumeratePhysicalDevices(context.Instance, ref devicedCount, null);

            if (devicedCount == 0)
            {
                throw new EngineException("failed to find GPUs with Vulkan support!");
            }

            var devices = new PhysicalDevice[devicedCount];
            fixed (PhysicalDevice* devicesPtr = devices)
            {
                context.Vk.EnumeratePhysicalDevices(context.Instance, ref devicedCount, devicesPtr);
            }

            foreach (var pDevice in devices)
            {
                var properties = context.Vk.GetPhysicalDeviceProperties(pDevice);
                var features = context.Vk.GetPhysicalDeviceFeatures(pDevice);
                var memoryProperties = context.Vk.GetPhysicalDeviceMemoryProperties(pDevice);

                bool supportsDeviceLocalHostVisible = false;
                for(int cnt = 0; cnt < memoryProperties.MemoryTypeCount;cnt++)
                {
                    if (((memoryProperties.MemoryTypes[cnt].PropertyFlags & MemoryPropertyFlags.HostVisibleBit) != 0) 
                        && ((memoryProperties.MemoryTypes[cnt].PropertyFlags & MemoryPropertyFlags.DeviceLocalBit) != 0))
                    {
                        supportsDeviceLocalHostVisible = true;
                        break;
                    }
                }

                var result = DeviceMeetsRequirements(pDevice, context, context.Surface!.Value, properties, features, requirements);
                if (!result.HasValue)
                {
                    continue;
                }

                _logger.LogInformation("Device Name: {dname}", Marshal.PtrToStringAnsi((nint)properties.DeviceName));
                _logger.LogInformation("Device Type: {dtype}", properties.DeviceType.ToString());
                _logger.LogInformation("GPU Driver Version: {dVersion}", SilkUtils.Version32ToString((Version32)properties.DriverVersion));
                _logger.LogInformation("Vulcan API Version: {vVersion}", SilkUtils.Version32ToString((Version32)properties.ApiVersion));

                for (var cnt = 0; cnt < memoryProperties.MemoryHeapCount; cnt++)
                {
                    float memorySizeGib = memoryProperties.MemoryHeaps[cnt].Size / 1024f / 1024f / 1024f;
                    if (memoryProperties.MemoryHeaps[cnt].Flags.HasFlag(MemoryHeapFlags.DeviceLocalBit))
                    {
                        _logger.LogInformation("Local GPU Memory: {mem} GiB", memorySizeGib.ToString("F2"));
                    }
                    else
                    {
                        _logger.LogInformation("Shared System Memory: {mem} GiB", memorySizeGib.ToString("F2"));
                    }
                }

                context.SetupDevice(new VulkanDevice()
                {
                    PhysicalDevice = pDevice,
                    QueueFamilyIndices = result.Value.Item1,
                    SwapchainSupport = result.Value.Item2,
                    Properties = properties,
                    Features = features,
                    Memory = memoryProperties,
                    SupportsDeviceLocalHostVisible = supportsDeviceLocalHostVisible,
                });
                return; //we have a device!

            }

            throw new EngineException("No physical device found which meet the requirements!");
        }

        private (PhysicalDeviceQueueFamilyInfo, VulkanSwapchainSupportInfo)? DeviceMeetsRequirements(
            PhysicalDevice device,
            VulkanContext context,
            SurfaceKHR surface,
            PhysicalDeviceProperties properties,
            PhysicalDeviceFeatures features,
            PhysicalDeviceRequirements requirements)
        {
            if (requirements.DiscreteGpu)
            {
                if (properties.DeviceType != PhysicalDeviceType.DiscreteGpu)
                    return null;
            }

            var queueFamilyInfoBuilder = FindQueueFamilies(device, context);

#if DEBUG
            _logger.LogDebug("Graphics | Present | Compute | Transfer | Name");
            var deviceName = Marshal.PtrToStringAnsi((nint)properties.DeviceName);
            _logger.LogDebug("{gFamilyIndex}|{pFamilyIndex}|{cFamilyIndex}|{tFamilyIndex}| {deviceName}",
                queueFamilyInfoBuilder.GraphicsFamilyIndex.ToString()?.PadLeft(9), queueFamilyInfoBuilder.PresentFamilyIndex.ToString()?.PadLeft(9), queueFamilyInfoBuilder.ComputeFamilyIndex.ToString()?.PadLeft(9), queueFamilyInfoBuilder.TransferFamilyIndex.ToString()?.PadLeft(10), deviceName);

#endif
            if (
                (!requirements.Graphics || queueFamilyInfoBuilder.GraphicsFamilyIndex.HasValue) &&
                (!requirements.Present || queueFamilyInfoBuilder.PresentFamilyIndex.HasValue) &&
                (!requirements.Compute || queueFamilyInfoBuilder.ComputeFamilyIndex.HasValue) &&
                (!requirements.Transfer || queueFamilyInfoBuilder.TransferFamilyIndex.HasValue)
                )
            {
                _logger.LogDebug("Device meets queue requirements");

                var swapChainSupport = QuerySwapChainSupport(device, context);
                if (swapChainSupport.Formats.Length < 1 || swapChainSupport.PresentModes.Length < 1)
                {
                    //not sufficient
                    return null;
                }

                if (requirements.DeviceExtensions.Length > 0 && !CheckDeviceExtensionsSupport(device, context, requirements.DeviceExtensions))
                {
                    return null;
                }

                if (requirements.SamplerAnisotropy && !features.SamplerAnisotropy)
                    return null;

                return (queueFamilyInfoBuilder.Build(), swapChainSupport);
            }

            return null;
        }

        private bool CheckDeviceExtensionsSupport(PhysicalDevice device, VulkanContext context, string[] deviceExtensions)
        {
            uint extentionsCount = 0;
            context.Vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, null);

            var availableExtensions = new ExtensionProperties[extentionsCount];
            fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
            {
                context.Vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, availableExtensionsPtr);
            }

            var availableExtensionNames = availableExtensions.Select(extension => Marshal.PtrToStringAnsi((nint)extension.ExtensionName)).ToHashSet();

            return deviceExtensions.All(availableExtensionNames.Contains);

        }

        private PhysicalDeviceQueueFamilyInfo.Builder FindQueueFamilies(PhysicalDevice device, VulkanContext context)
        {
            var queueFamilyInfo = new PhysicalDeviceQueueFamilyInfo.Builder();

            uint queueFamilityCount = 0;
            context.Vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

            var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
            fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
            {
                context.Vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
            }

            ushort minTransferScore = ushort.MaxValue;
            uint i = 0;
            foreach (var queueFamily in queueFamilies)
            {
                ushort currentTransferScore = 0;
                //graphic queue?
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                {
                    queueFamilyInfo.GraphicsFamilyIndex = i;
                    currentTransferScore++;
                }
                //compute queue?
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.ComputeBit))
                {
                    queueFamilyInfo.ComputeFamilyIndex = i;
                    currentTransferScore++;
                }

                //transfer queue?
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.TransferBit))
                {
                    //take the index if it is the current lowest
                    if (currentTransferScore <= minTransferScore)
                    {
                        queueFamilyInfo.TransferFamilyIndex = i;
                        minTransferScore = currentTransferScore;
                    }
                }
                context.KhrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, context.Surface!.Value, out var presentSupport);

                if (presentSupport)
                {
                    queueFamilyInfo.PresentFamilyIndex = i;
                }

                i++;
            }

            return queueFamilyInfo;
        }
    }
}
