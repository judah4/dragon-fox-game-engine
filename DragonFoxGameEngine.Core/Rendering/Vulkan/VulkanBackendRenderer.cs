using DragonGameEngine.Core;
using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Exceptions.Vulkan;
using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders;
using DragonGameEngine.Core.Rendering.Vulkan.Shaders;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using Microsoft.Extensions.Logging;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Windowing;
using System;
using System.Runtime.InteropServices;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace DragonGameEngine.Core.Rendering.Vulkan
{
    public unsafe sealed class VulkanBackendRenderer : IRenderer
    {
        private readonly ILogger _logger;
        private readonly string _applicationName;
        private readonly IWindow _window;

        private readonly VulkanDeviceSetup _deviceSetup;
        private readonly VulkanSwapchainSetup _swapchainSetup;
        private readonly VulkanImageSetup _imageSetup;
        private readonly VulkanRenderpassSetup _renderpassSetup;
        private readonly VulkanCommandBufferSetup _commandBufferSetup;
        private readonly VulkanFramebufferSetup _framebufferSetup;
        private readonly VulkanFenceSetup _fenceSetup;
        private readonly VulkanShaderManager _shaderManager;
        private readonly VulkanMaterialShaderManager _materialShaderManager;
        private readonly VulkanUiShaderManager _uiShaderManager;
        private readonly VulkanPipelineSetup _pipelineSetup;
        private readonly VulkanBufferManager _bufferManager;

#if DEBUG
        private readonly bool EnableValidationLayers = true; //enable when tools are installed. Add to config
#else
        private readonly bool EnableValidationLayers = false;
#endif

        private readonly string[] _validationLayers = new[]
{
            "VK_LAYER_KHRONOS_validation"
        };

        private VulkanContext? _context;

        private Vector2D<uint> _cachedFramebufferSize;

        public VulkanBackendRenderer(string applicationName, IWindow window, TextureSystem textureSystem, ResourceSystem resourceSystem, ILogger logger)
        {
            _applicationName = applicationName;
            _window = window;
            _logger = logger;
            _deviceSetup = new VulkanDeviceSetup(logger);
            _imageSetup = new VulkanImageSetup(logger);
            _swapchainSetup = new VulkanSwapchainSetup(logger, _deviceSetup, _imageSetup);
            _renderpassSetup = new VulkanRenderpassSetup(logger);
            _commandBufferSetup = new VulkanCommandBufferSetup(logger);
            _framebufferSetup = new VulkanFramebufferSetup(logger);
            _fenceSetup = new VulkanFenceSetup(logger);
            _bufferManager = new VulkanBufferManager(_imageSetup, _commandBufferSetup, logger);

            //shaders
            _shaderManager = new VulkanShaderManager(resourceSystem);
            _pipelineSetup = new VulkanPipelineSetup(logger);
            _materialShaderManager = new VulkanMaterialShaderManager(logger, _shaderManager, _pipelineSetup, _bufferManager, textureSystem);
            _uiShaderManager = new VulkanUiShaderManager(logger, _shaderManager, _pipelineSetup, _bufferManager, textureSystem);

        }

        public void Init()
        {
            if (_context != null)
            {
                throw new EngineException("Vulkan is already initialized!");
            }

            var vk = Vk.GetApi();

            var gameVersion = ApplicationInfo.GameVersion;
            var engineVersion = ApplicationInfo.EngineVersion;
            Silk.NET.Vulkan.ApplicationInfo appInfo = new()
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(_applicationName),
                ApplicationVersion = new Version32((uint)gameVersion.Major, (uint)gameVersion.Minor, (uint)gameVersion.Revision),
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi(ApplicationInfo.GetGameEngineName()),
                EngineVersion = new Version32((uint)engineVersion.Major, (uint)engineVersion.Minor, (uint)engineVersion.Revision),
                ApiVersion = Vk.Version12,
            };

            InstanceCreateInfo createInfo = new()
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo
            };

            var extensions = VulkanPlatform.GetRequiredExtensions(_window);

#if DEBUG
            _logger.LogDebug("Required Extensions: {extentionsFormatted}", string.Join(",", extensions));
#endif

            createInfo.EnabledExtensionCount = (uint)extensions.Length;
            createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);

            if (EnableValidationLayers)
            {
                uint availableLayerCount = 0;
                vk.EnumerateInstanceLayerProperties(ref availableLayerCount, null); //get count
                var availableLayers = new LayerProperties[availableLayerCount];
                fixed (LayerProperties* availableLayersPtr = availableLayers)
                {
                    if (vk.EnumerateInstanceLayerProperties(ref availableLayerCount, availableLayersPtr) == Result.Success)
                    {
                        foreach (var layer in _validationLayers)
                        {
                            bool found = false;

                            for (int cnt = 0; cnt < availableLayerCount; cnt++)
                            {
                                var availableLayer = availableLayersPtr[cnt];

                                var availableName = Marshal.PtrToStringAnsi((nint)availableLayer.LayerName);
                                if (availableName == layer)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                throw new EngineException($"Required Validation Layer is missing: {layer}");
                            }
                            _logger.LogDebug("Validation Layer {layer} added.", layer);
                        }
                    }
                }

                createInfo.EnabledLayerCount = (uint)_validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(_validationLayers);

                DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
                PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
                createInfo.PNext = &debugCreateInfo;

            }
            else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PNext = null;
            }

            AllocationCallbacks* allocator = null; //null for now. Might deal with it later
            var createInstanceResult = vk.CreateInstance(createInfo, allocator, out var instance);
            if (createInstanceResult != Result.Success)
            {
                throw new VulkanResultException(createInstanceResult, "failed to create Vulkan instance!");
            }
            ExtDebugUtils? debugUtils = null;
            DebugUtilsMessengerEXT debugMessenger = default;
            if (EnableValidationLayers)
            {
                var debuggingUtils = SetupDebugMessenger(vk, instance);
                if (debuggingUtils.HasValue)
                {
                    debugUtils = debuggingUtils.Value.Item1;
                    debugMessenger = debuggingUtils.Value.Item2;
                }
            }

            Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
            Marshal.FreeHGlobal((nint)appInfo.PEngineName);
            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

            if (EnableValidationLayers)
            {
                SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
            }

            var framebufferSize = new Vector2D<uint>((uint)_window.Size.X, (uint)_window.Size.Y);
            _cachedFramebufferSize = framebufferSize;
            _context = new VulkanContext(vk, _window, instance, allocator, debugUtils, debugMessenger, framebufferSize);

            VulkanPlatform.CreateSurface(_context, _logger);

            //Device creation
            _deviceSetup.Create(_context);

            //Swapchain
            var swapchain = _swapchainSetup.Create(_context, _context.FramebufferSize);

            //World Renderpass
            _context.SetupMainRenderpass(
                _renderpassSetup.Create(
                    _context,
                    new Rect2D(new Offset2D(0, 0), new Extent2D(_context.FramebufferSize.X, _context.FramebufferSize.Y)),
                    System.Drawing.Color.CornflowerBlue,
                    1.0f, 
                    0,
                    RenderpassClearFlags.ClearColorBufferFlag | RenderpassClearFlags.ClearDepthBufferFlag | RenderpassClearFlags.ClearStencilBufferFlag,
                    false,
                    true));

            //UI renderpass
            _context.SetupUiRenderpass(
                _renderpassSetup.Create(
                    _context,
                    new Rect2D(new Offset2D(0, 0), new Extent2D(_context.FramebufferSize.X, _context.FramebufferSize.Y)),
                    System.Drawing.Color.Transparent,
                    1.0f,
                    0,
                    RenderpassClearFlags.None,
                    true,
                    false));

            //Create frame buffers.
            _context.SetupSwapchain(swapchain);
            swapchain = RegenerateFramebuffers();
            _context.SetupSwapchain(swapchain); //this all feels real nasty but it works I guess

            //Create command buffers
            CreateCommandBuffers();

            CreateSemaphoresAndFences();

            var materialShader = _materialShaderManager.Create(_context);
            _context.SetupBuiltinMaterialShader(materialShader);
            var uiShader = _uiShaderManager.Create(_context);
            _context.SetupBuiltinUiShader(uiShader); //this is probably the only time we need to set thiss

            CreateBuffers(_context);

            Array.Fill(_context.Geometries, new VulkanGeometryData(EntityIdService.INVALID_ID));

            _logger.LogInformation($"Vulkan initialized.");
        }

        public void Shutdown()
        {
            if (_context == null)
                return;

            //wait until the device is idle again
            _context.Vk.DeviceWaitIdle(_context.Device.LogicalDevice);

            DestroyBuffers(_context);

            _uiShaderManager.Destroy(_context, _context.UiShader!);
            _materialShaderManager.Destroy(_context, _context.MaterialShader!);

            DestroySemaphoresAndFences();

            CleanUpCommandBuffers();

            if(_context.Swapchain != null)
            {
                var swapchain = DestroyFramebuffers(_context.Swapchain!);
                _context.SetupSwapchain(swapchain); //this all feels real nasty but it works I guess
            }

            _renderpassSetup.Destory(_context, _context.UiRenderPass!);
            _renderpassSetup.Destory(_context, _context.MainRenderPass!);

            _swapchainSetup.Destroy(_context, _context.Swapchain!);

            _deviceSetup.Destroy(_context);

            if (EnableValidationLayers)
            {
                //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
                _context!.DebugUtils?.DestroyDebugUtilsMessenger(_context.Instance, _context.DebugMessenger, null);
            }

            if (_context.Surface.HasValue)
            {
                _context.KhrSurface!.DestroySurface(_context.Instance, _context.Surface.Value, _context.Allocator);
            }

            _context?.Vk.DestroyInstance(_context.Instance, _context.Allocator);
            _context?.Vk.Dispose();
            _context = null;
            _logger.LogInformation("Vulkan is shutdown.");
        }

        public void Resized(Vector2D<uint> size)
        {
            if (_context == null)
                return;

            if (_context.FramebufferSize == size)
                return;

            //do resizing
            _cachedFramebufferSize = size;
            _context.SetFramebufferSize(_context.FramebufferSize, _context.FramebufferSizeGeneration + 1);

            _logger.LogDebug("Vulkan Backend Renderer resized {size}, generation {framebufferSizeGeneration}", size, _context.FramebufferSizeGeneration);
        }

        public bool BeginFrame(double deltaTime)
        {
            if (_context == null)
            {
                return false;
            }
            _context.SetFrameDeltaTime(deltaTime);
            var device = _context.Device;
            if (_context.RecreatingSwapchain)
            {
                var result = _context.Vk.DeviceWaitIdle(device.LogicalDevice);
                if (!VulkanUtils.ResultIsSuccess(result))
                {
                    _logger.LogError("BeginFrame DeviceWaitIdle failed: {formattedError}", VulkanUtils.FormattedResult(result));
                    return false;
                }
                _logger.LogDebug("Recreating swapchain, booting");
                return false;
            }

            //Check if the framebuffer has been resized. If so, a new swapchain must be created.
            if (_context.FramebufferSizeGeneration != _context.FramebufferSizeGenerationLastGeneration)
            {
                var result = _context.Vk.DeviceWaitIdle(device.LogicalDevice);
                if (!VulkanUtils.ResultIsSuccess(result))
                {
                    _logger.LogError("BeginFrame DeviceWaitIdle failed: {formattedError}", VulkanUtils.FormattedResult(result));
                    return false;
                }

                if (!RecreateSwapchain())
                {
                    _logger.LogDebug("Recreate swapchain failed");
                    return false;
                }

                _logger.LogDebug("Resized, booting");
                return false;
            }

            // Wait for the current frame to complete. The fence being free will allow this one to move on.
            var fenceResult = _fenceSetup.FenceWait(_context, _context.InFlightFences![_context.CurrentFrame], ulong.MaxValue);
            if (fenceResult.IsFailure)
            {
                _logger.LogWarning("In-flight fence wait failure!");
                return false;
            }
            _context.InFlightFences[_context.CurrentFrame] = fenceResult.Value; //set the fence in the array

            //Acquire the next image from the swapchain. Pass along the semaphore that should signal when this completes.
            //This same semaphore will alter be waited on by the queue submission to ensure this image is available.
            var imageIndex = _swapchainSetup.AquireNextImageIndex(_context, _context.Swapchain!, ulong.MaxValue, _context.ImageAvailableSemaphores![_context.CurrentFrame], default);
            _context.SetImageIndex(imageIndex);

            //begin reporting commands
            var commandBuffer = _context.GraphicsCommandBuffers![_context.ImageIndex];
            commandBuffer = _commandBufferSetup.CommandBufferReset(_context, commandBuffer);
            commandBuffer = _commandBufferSetup.CommandBufferBegin(_context, commandBuffer, false, false, false);
            _context.GraphicsCommandBuffers[_context.ImageIndex] = commandBuffer;

            //dynamic state
            Viewport viewport = new()
            {
                X = 0,
                Y = _context.FramebufferSize.Y,
                Width = _context.FramebufferSize.X,
                Height = -_context.FramebufferSize.Y, //flip to be with OpenGL
                MinDepth = 0.0f,
                MaxDepth = 1.0f,
            };

            //Scissor
            Rect2D scissor = new()
            {
                Offset = { X = 0, Y = 0 },
                Extent = new Extent2D(_context.FramebufferSize.X, _context.FramebufferSize.Y),
            };

            _context.Vk.CmdSetViewport(commandBuffer.Handle, 0, 1, viewport);
            _context.Vk.CmdSetScissor(commandBuffer.Handle, 0, 1, scissor);

            var renderPass = _context.MainRenderPass!;
            var rect = renderPass.Rect;
            rect.Extent = scissor.Extent; //hacky but sets it at least
            renderPass.Rect = rect;
            _context.SetupMainRenderpass(renderPass);

            var uiRenderPass = _context.UiRenderPass!;
            rect = uiRenderPass.Rect;
            rect.Extent = scissor.Extent; //hacky but sets it at least
            uiRenderPass.Rect = rect;
            _context.SetupUiRenderpass(uiRenderPass);

            //we started the frame!

            return true;
        }

        public void EndFrame(double deltaTime)
        {
            if (_context == null)
            {
                return;
            }

            var commandBuffer = _context.GraphicsCommandBuffers![_context.ImageIndex];

            commandBuffer = _commandBufferSetup.CommandBufferEnd(_context, commandBuffer);
            _context.GraphicsCommandBuffers[_context.ImageIndex] = commandBuffer;

            //make sure the previous frame is not using this image
            if (_context.ImagesInFlight![_context.ImageIndex].Handle.Handle != 0)
            {
                _fenceSetup.FenceWait(_context, _context.ImagesInFlight![_context.CurrentFrame], ulong.MaxValue);
            }

            //mark in use
            _context.ImagesInFlight[_context.ImageIndex] = _context.InFlightFences![_context.CurrentFrame];

            _context.InFlightFences[_context.CurrentFrame] = _fenceSetup.FenceReset(_context, _context.InFlightFences[_context.CurrentFrame]);

            var commandBufferHande = commandBuffer.Handle;
            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &commandBufferHande,
            };

            var signalSemaphores = stackalloc[] { _context.QueueCompleteSemaphores![_context.CurrentFrame] };
            // The semaphore(s) to be signaled when the queue is complete.
            submitInfo.SignalSemaphoreCount = 1;
            submitInfo.PSignalSemaphores = signalSemaphores;

            var waitSemaphores = stackalloc[] { _context.ImageAvailableSemaphores![_context.CurrentFrame] };
            // Wait semaphore ensures that the operation cannot begin until the image is available.
            submitInfo.WaitSemaphoreCount = 1;
            submitInfo.PWaitSemaphores = waitSemaphores;

            var flags = new PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit };
            fixed (PipelineStageFlags* flagsPtr = flags)
            {
                submitInfo.PWaitDstStageMask = flagsPtr;
            }

            var submitResult = _context.Vk.QueueSubmit(_context.Device.GraphicsQueue, 1, submitInfo, _context.InFlightFences[_context.CurrentFrame].Handle);
            if (!VulkanUtils.ResultIsSuccess(submitResult))
            {
                _logger.LogError($"QueueSubmit failed: {VulkanUtils.FormattedResult(submitResult)}");
                return;
            }

            commandBuffer = _commandBufferSetup.CommandBufferUpdateSubmitted(_context, commandBuffer);
            _context.GraphicsCommandBuffers[_context.ImageIndex] = commandBuffer;
            //End queue submission

            _swapchainSetup.Present(_context, _context.Swapchain!, _context.Device.GraphicsQueue, _context.Device.PresentQueue, signalSemaphores, _context.ImageIndex);

            return;
        }

        public bool BeginRenderpass(RenderpassId renderpassId)
        {
            if(_context == null)
            {
                return false;
            }

            VulkanRenderpass renderpass;
            Framebuffer framebuffer;

            if(renderpassId == RenderpassId.World)
            {
                renderpass = _context.MainRenderPass!;
                framebuffer = _context.WorldFramebuffers[_context.ImageIndex];
            }
            else if (renderpassId == RenderpassId.Ui)
            {
                renderpass = _context.UiRenderPass!;
                framebuffer = _context.Swapchain!.Framebuffers[_context.ImageIndex];
            }
            else
            {
                throw new EngineException($"Renderpass {renderpassId} is not valid!");
            }

            var commandBuffer = _context.GraphicsCommandBuffers![_context.ImageIndex];

            //Being the render pass
            commandBuffer = _renderpassSetup.BeginRenderpass(_context, commandBuffer, renderpass, framebuffer);
            _context.GraphicsCommandBuffers[_context.ImageIndex] = commandBuffer;

            //Use the appropriate shader
            if (renderpassId == RenderpassId.World)
            {
                _materialShaderManager.ShaderUse(_context, _context.MaterialShader!);
            }
            else if (renderpassId == RenderpassId.Ui)
            {
                _uiShaderManager.ShaderUse(_context, _context.UiShader!);
            }

            return true;
        }

        public void EndRenderpass(RenderpassId renderpassId)
        {
            if (_context == null)
            {
                return;
            }

            VulkanRenderpass renderpass;

            if (renderpassId == RenderpassId.World)
            {
                renderpass = _context.MainRenderPass!;
            }
            else if (renderpassId == RenderpassId.Ui)
            {
                renderpass = _context.UiRenderPass!;
            }
            else
            {
                throw new EngineException($"Renderpass {renderpassId} is not valid!");
            }

            var commandBuffer = _context.GraphicsCommandBuffers![_context.ImageIndex];

            //End renderpass
            commandBuffer = _renderpassSetup.EndRenderpass(_context, commandBuffer, renderpass);
            _context.GraphicsCommandBuffers![_context.ImageIndex] = commandBuffer;
        }


        public void UpdateGlobalWorldState(Matrix4X4<float> projection, Matrix4X4<float> view, Vector3D<float> viewPosition, System.Drawing.Color ambientColor, int mode)
        {
            var shader = _context!.MaterialShader!;

            _materialShaderManager.ShaderUse(_context, shader);

            //update the view and projection
            shader.GlobalUbo = new VulkanMaterialShaderGlobalUniformObject()
            {
                Projection = projection,
                View = view,
            };
            //TODO: other ubo properties

            _context.SetupBuiltinMaterialShader(shader);

            _materialShaderManager.UpdateGlobalState(_context, shader, _context.FrameDeltaTime);
        }

        public void UpdateGlobalUiState(Matrix4X4<float> projection, Matrix4X4<float> view, int mode)
        {
            var shader = _context!.UiShader!;

            _uiShaderManager.ShaderUse(_context, shader);

            //update the view and projection
            shader.GlobalUbo = new VulkanUiShaderGlobalUniformObject()
            {
                Projection = projection,
                View = view,
            };
            //TODO: other ubo properties

            _context.SetupBuiltinUiShader(shader);

            _uiShaderManager.UpdateGlobalState(_context, shader, _context.FrameDeltaTime);
        }

        public void LoadTexture(Span<byte> pixels, Texture texture)
        {
            if(_context == null)
            {
                return;
            }
            var imageSize = texture.Size.X * texture.Size.Y * texture.ChannelCount;

            //Assume 8 bits per channel
            var imageFormat = Format.R8G8B8A8Unorm;

            var usage = BufferUsageFlags.TransferSrcBit;
            var memFlags = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit;
            var staging = _bufferManager.BufferCreate(_context, imageSize, usage, memFlags, true);

            _bufferManager.BufferLoadData(_context, staging, 0, imageSize, 0, pixels);

            uint mipLevels = 1U;

            //lots of assumptions
            var image = _imageSetup.ImageCreate(
                _context,
                ImageType.Type2D,
                texture.Size,
                mipLevels,
                imageFormat,
                ImageTiling.Optimal,
                ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit | ImageUsageFlags.ColorAttachmentBit,
                MemoryPropertyFlags.DeviceLocalBit,
                true,
                ImageAspectFlags.ColorBit);

            var pool = _context.Device.GraphicsCommandPool;
            var queue = _context.Device.GraphicsQueue;
            var tempBuffer = _commandBufferSetup.CommandBufferAllocateAndBeginSingleUse(_context, pool);

            _imageSetup.TransitionLayout(_context, tempBuffer, image, imageFormat, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);

            //copy from the buffer
            _imageSetup.CopyFromBuffer(_context, image, staging.Handle, tempBuffer);

            //transition from optimal for data reciept to shader read only optimal layout
            _imageSetup.TransitionLayout(_context, tempBuffer, image, imageFormat, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

            _commandBufferSetup.CommandBufferEndSingleUse(_context, pool, tempBuffer, queue);

            _bufferManager.BufferDestroy(_context, staging);

            SamplerCreateInfo samplerInfo = new()
            {
                SType = StructureType.SamplerCreateInfo,
                MagFilter = Filter.Linear,
                MinFilter = Filter.Linear,
                AddressModeU = SamplerAddressMode.Repeat,
                AddressModeV = SamplerAddressMode.Repeat,
                AddressModeW = SamplerAddressMode.Repeat,
                AnisotropyEnable = true,
                MaxAnisotropy = 16,
                BorderColor = BorderColor.IntOpaqueBlack,
                UnnormalizedCoordinates = false,
                CompareEnable = false,
                CompareOp = CompareOp.Always,
                MipmapMode = SamplerMipmapMode.Linear,
                MinLod = 0,
                MaxLod = mipLevels,
                MipLodBias = 0,
            };

            Sampler sampler;
            var samplerResult = _context.Vk.CreateSampler(_context.Device.LogicalDevice, samplerInfo, _context.Allocator, &sampler);
            if (samplerResult != Result.Success)
            {
                throw new VulkanResultException(samplerResult, "Failed to create texture sampler!");
            }

            var data = new VulkanTextureData(image, sampler);

            texture.UpdateTextureInternalData(data);
        }

        public void DestroyTexture(Texture texture)
        {
            if(_context == null)
            {
                return;
            }
            if (texture.InternalData is not VulkanTextureData)
            {
                return;
            }

            _context.Vk.DeviceWaitIdle(_context.Device.LogicalDevice);

            var data = (VulkanTextureData)texture.InternalData;
            
            _imageSetup.ImageDestroy(_context, data.Image);

            _context.Vk.DestroySampler(_context.Device.LogicalDevice, data.Sampler, _context.Allocator);

            //default some values again;
            texture.ResetGeneration();
            texture.UpdateTextureInternalData(new object());
        }

        public void LoadMaterial(Material material)
        {
            if(_context == null || _context.MaterialShader == null || _context.UiShader == null)
            {
                return;
            }

            switch(material.MaterialType)
            {
                case MaterialType.World:
                    _materialShaderManager.AcquireResources(_context, _context.MaterialShader, material);
                    break;
                case MaterialType.Ui:
                    _uiShaderManager.AcquireResources(_context, _context.UiShader, material);
                    break;
            }

            _logger.LogTrace("Renderer: {matType} Material '{name}' ({instanceId}) Created", material.MaterialType, material.Name, material.InternalId);
        }

        public void DestroyMaterial(Material material)
        {
            if (_context == null || _context.MaterialShader == null || _context.UiShader == null)
            {
                return;
            }

            switch (material.MaterialType)
            {
                case MaterialType.World:
                    _materialShaderManager.ReleaseResources(_context, _context.MaterialShader, material);
                    break;
                case MaterialType.Ui:
                    _uiShaderManager.ReleaseResources(_context, _context.UiShader, material);
                    break;
            }

            _logger.LogTrace("Renderer: {matType} Material Destroyed", material.MaterialType);
        }

        public void LoadGeometry(Geometry geometry, Vertex3d[] vertices, uint[] indices)
        {
            if (_context == null)
            {
                return;
            }
            bool isReupload = geometry.InternalId != EntityIdService.INVALID_ID;
            VulkanGeometryData oldRange = new VulkanGeometryData(EntityIdService.INVALID_ID);

            VulkanGeometryData internalData = new VulkanGeometryData(EntityIdService.INVALID_ID);

            if (isReupload)
            {
                internalData = _context.Geometries[geometry.InternalId];
                //make a copy
                oldRange = internalData;
            }
            else
            {
                for(uint cnt = 0; cnt < _context.Geometries.Length; cnt++)
                {
                    if (_context.Geometries[cnt].Id !=  EntityIdService.INVALID_ID)
                    {
                        continue;
                    }

                    geometry.UpdateInternalId(cnt);
                    internalData = new VulkanGeometryData(cnt, EntityIdService.INVALID_ID, 0, 0, 0, 0, 0, 0);
                    _context.Geometries[cnt] = internalData;
                    break;
                }
            }

            if(internalData.Id == EntityIdService.INVALID_ID)
            {
                _logger.LogError("LoadGeometry failed to find a free index for a new geometry upload. Adjust config to allow for more.");
                return;
            }

            var pool = _context.Device.GraphicsCommandPool;
            var queue = _context.Device.GraphicsQueue;

            // Vertex data.
            var vertexSize = (uint)sizeof(Vertex3d) * (uint)vertices.Length;
            var vertexDataResult = UploadDataRange<Vertex3d>(_context, pool, default, queue, _context.ObjectVertexBuffer, vertexSize, vertices);
            if(vertexDataResult.IsFailure)
            {
                _logger.LogError(vertexDataResult.Error);
                return;
            }
            var indexSize = 0U;
            var indexDataResult = Foxis.Library.Result.Ok<ulong>();
            if (indices.Length > 0)
            {
                // Index data, if applicable
                indexSize = (uint)sizeof(uint) * (uint)indices.Length;
                indexDataResult = UploadDataRange<uint>(_context, pool, default, queue, _context.ObjectIndexBuffer, indexSize, indices);
                if (indexDataResult.IsFailure)
                {
                    _logger.LogError(indexDataResult.Error);
                    return;
                }
                // TODO: should maintain a free list instead of this.
            }

            var generation = 0U;

            if (internalData.Generation != EntityIdService.INVALID_ID)
            {
                generation++;
            }

            _context.Geometries[geometry.InternalId] = new VulkanGeometryData(
                geometry.InternalId,
                generation,
                (uint)vertices.Length,
                vertexSize,
                vertexDataResult.Value,
                (uint)indices.Length,
                indexSize,
                indexDataResult.Value);

            if (isReupload && oldRange.Id != EntityIdService.INVALID_ID)
            {
                // Free vertex data
                FreeDataRange(_context.ObjectVertexBuffer, oldRange.VertexBufferOffset, oldRange.VertexSize);

                // Free index data, if applicable
                if (oldRange.IndexSize > 0)
                {
                    FreeDataRange(_context.ObjectIndexBuffer, oldRange.IndexBufferOffset, oldRange.IndexSize);
                }
            }
        }

        public void DestroyGeometry(Geometry geometry)
        {
            if (_context == null || geometry.InternalId == EntityIdService.INVALID_ID)
            {
                return;
            }
            
            _context.Vk.DeviceWaitIdle(_context.Device.LogicalDevice);
            var internal_data = _context.Geometries[geometry.InternalId];

            // Free vertex data
            FreeDataRange(_context.ObjectVertexBuffer, internal_data.VertexBufferOffset, internal_data.VertexSize);

            // Free index data, if applicable
            if (internal_data.IndexSize > 0)
            {
                FreeDataRange(_context.ObjectIndexBuffer, internal_data.IndexBufferOffset, internal_data.IndexSize);
            }

            // Clean up data.
            _context.Geometries[geometry.InternalId] = new VulkanGeometryData(EntityIdService.INVALID_ID);
            geometry.UpdateInternalId(EntityIdService.INVALID_ID);
        }

        public void DrawGeometry(GeometryRenderData data)
        {
            if (_context == null || _context.MaterialShader == null || _context.UiShader == null || data.Geometry == null || data.Geometry.InternalId == EntityIdService.INVALID_ID)
            {
                return;
            }

            var bufferData = _context.Geometries[data.Geometry.InternalId];

            //assume we always have a material set, even if it's the default material
            var material = data.Geometry.Material;
            switch (data.Geometry.Material.MaterialType)
            {
                case MaterialType.World:
                    _materialShaderManager.SetModel(_context, _context.MaterialShader, data.Model);
                    _materialShaderManager.ApplyMaterial(_context, _context.MaterialShader, material);
                    break;
                case MaterialType.Ui:
                    _uiShaderManager.SetModel(_context, _context.UiShader, data.Model);
                    _uiShaderManager.ApplyMaterial(_context, _context.UiShader, material);
                    break;
                default:
                    _logger.LogError("Unknown material type for draw: {matType} {matName}", material.MaterialType, material.Name);
                    return;
            }

            var commandBuffer = _context!.GraphicsCommandBuffers![_context.ImageIndex];

            var vertexBuffers = new Buffer[] { _context.ObjectVertexBuffer.Handle };
            //bind vertex buffer at offset
            var offsets = new ulong[] { bufferData.VertexBufferOffset };

            fixed (ulong* offsetsPtr = offsets)
            fixed (Buffer* vertexBuffersPtr = vertexBuffers)
            {
                _context.Vk.CmdBindVertexBuffers(commandBuffer.Handle, 0, 1, vertexBuffersPtr, offsetsPtr);
            }

            if(bufferData.IndexCount > 0)
            {
                //bind index buffer at offset
                _context.Vk.CmdBindIndexBuffer(commandBuffer.Handle, _context.ObjectIndexBuffer.Handle, bufferData.IndexBufferOffset, IndexType.Uint32);

                //Issue the draw
                _context.Vk.CmdDrawIndexed(commandBuffer.Handle, bufferData.IndexCount, 1, 0, 0, 0);
            }
            else
            {
                //Issue the draw sans indicies
                _context.Vk.CmdDraw(commandBuffer.Handle, bufferData.VertexCount, 1, 0, 0);
            }

            //drawCall++
        }

        private void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
        {
            createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
            createInfo.MessageSeverity = //DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
            createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
            createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
        }

        /// <summary>
        /// Debugging messaging.
        /// </summary>
        /// <param name="vk"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <remarks>I'm not 100% what this does -Judah</remarks>
        private (ExtDebugUtils, DebugUtilsMessengerEXT)? SetupDebugMessenger(Vk vk, Instance instance)
        {
            if (!EnableValidationLayers) return null;

            ExtDebugUtils debugUtils;
            DebugUtilsMessengerEXT debugMessenger;

            //TryGetInstanceExtension equivilant to method CreateDebugUtilsMessengerEXT from original tutorial.
            if (!vk.TryGetInstanceExtension(instance, out debugUtils)) return null;

            DebugUtilsMessengerCreateInfoEXT createInfo = new();
            PopulateDebugMessengerCreateInfo(ref createInfo);

            var createDebugResult = debugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out debugMessenger);
            if (createDebugResult != Result.Success)
            {
                throw new VulkanResultException(createDebugResult, "failed to set up debug messenger!");
            }
            _logger.LogDebug($"Debug messenger setup.");
            return (debugUtils, debugMessenger);
        }

        /// <summary>
        /// Send debugging info to console
        /// </summary>
        /// <param name="messageSeverity"></param>
        /// <param name="messageTypes"></param>
        /// <param name="pCallbackData"></param>
        /// <param name="pUserData"></param>
        /// <returns></returns>
        private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
        {
            _logger.LogDebug("validation layer: {message}", Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

            return Vk.False;
        }

        /// <summary>
        /// Create our primary command buffers
        /// </summary>
        private void CreateCommandBuffers()
        {
            if(_context == null || _context.Swapchain == null)
            {
                return;
            }

            var commandBuffers = _context!.GraphicsCommandBuffers;
            if (commandBuffers == null || commandBuffers.Length != _context.Swapchain.SwapchainImages!.Length)
            {
                //destroy previous if needed?
                commandBuffers = new VulkanCommandBuffer[_context.Swapchain.SwapchainImages!.Length];
            }

            for (int cnt = 0; cnt < commandBuffers.Length; cnt++)
            {
                if (commandBuffers[cnt].Handle.Handle != 0)
                {
                    commandBuffers[cnt] = _commandBufferSetup.CommandBufferFree(_context, _context.Device.GraphicsCommandPool, commandBuffers[cnt]);
                }

                commandBuffers[cnt] = _commandBufferSetup.CommandBufferAllocate(_context, _context.Device.GraphicsCommandPool, true);
            }

            _context.SetupGraphicsCommandBuffers(commandBuffers);
            _logger.LogDebug("Graphics command buffers created.");
        }

        /// <summary>
        /// Free our primary command buffers
        /// </summary>
        private void CleanUpCommandBuffers()
        {
            var commandBuffers = _context!.GraphicsCommandBuffers;
            if (commandBuffers == null)
                return;

            for (int cnt = 0; cnt < commandBuffers.Length; cnt++)
            {
                if (commandBuffers[cnt].Handle.Handle != 0)
                {
                    commandBuffers[cnt] = _commandBufferSetup.CommandBufferFree(_context, _context.Device.GraphicsCommandPool, commandBuffers[cnt]);
                }
            }

            _context.SetupGraphicsCommandBuffers(commandBuffers);
            _logger.LogDebug("Graphics command buffers cleaned up.");
        }

        private VulkanSwapchain RegenerateFramebuffers()
        {
            if (_context == null || _context.Swapchain == null)
            {
                throw new EngineException("Context is not set up. Was RegenerateFramebuffers called before Vulkan is initialized?");
            }
            if (_context.Swapchain.ImageViews == null)
            {
                _logger.LogWarning("Image views are emtpy for regenerating frame buffers. Why?");
                return _context.Swapchain;
            }

            var imageCount = _context.Swapchain.ImageViews.Length;
            for (int cnt = 0; cnt < imageCount; cnt++)
            {
                var worldAttachments = new[]
                {
                    _context.Swapchain.ImageViews[cnt],
                    _context.Swapchain.DepthAttachment.ImageView,
                };

                _context.WorldFramebuffers[cnt] = _framebufferSetup.FramebufferCreate(_context, _context.MainRenderPass!, _context.FramebufferSize, worldAttachments);

                // Swapchain framebuffers (UI pass). outputs to swapchain images
                var uiAttachments = new[]
                {
                    _context.Swapchain.ImageViews[cnt],
                };
                _context.Swapchain.Framebuffers[cnt] = _framebufferSetup.FramebufferCreate(_context, _context.UiRenderPass!, _context.FramebufferSize, uiAttachments);
            }
            _logger.LogDebug($"Regening Frame buffers with size {_context!.FramebufferSize}");
            return _context.Swapchain;
        }

        private VulkanSwapchain DestroyFramebuffers(VulkanSwapchain swapchain)
        {
            if(_context == null || _context.Swapchain == null)
            {
                return swapchain;
            }    
            for (int cnt = 0; cnt < _context.Swapchain.ImageViews!.Length; cnt++)
            {
                _context.WorldFramebuffers[cnt] = _framebufferSetup.FramebufferDestroy(_context, _context.WorldFramebuffers[cnt]);
                swapchain.Framebuffers[cnt] = _framebufferSetup.FramebufferDestroy(_context, swapchain.Framebuffers[cnt]);
            }
            return swapchain;
        }

        private void CreateSemaphoresAndFences()
        {
            if (_context == null || _context.Swapchain == null)
            {
                return;
            }
            var imageAvailableSemaphores = new Semaphore[_context.Swapchain.MaxFramesInFlight];
            var renderFinishedSemaphores = new Semaphore[_context.Swapchain.MaxFramesInFlight];
            var inFlightFences = new VulkanFence[_context.Swapchain.MaxFramesInFlight];

            SemaphoreCreateInfo semaphoreInfo = new()
            {
                SType = StructureType.SemaphoreCreateInfo,
            };

            for (var i = 0; i < _context.Swapchain.MaxFramesInFlight; i++)
            {
                var semaphoreResult = _context.Vk.CreateSemaphore(_context.Device.LogicalDevice, semaphoreInfo, _context.Allocator, out imageAvailableSemaphores[i]);
                if (semaphoreResult != Result.Success)
                {
                    throw new VulkanResultException(semaphoreResult, "Failed to create image available semaphore for a frame!");
                }
                semaphoreResult = _context.Vk.CreateSemaphore(_context.Device.LogicalDevice, semaphoreInfo, _context.Allocator, out renderFinishedSemaphores[i]);
                if (semaphoreResult != Result.Success)
                {
                    throw new VulkanResultException(semaphoreResult, "Failed to create rendered finished semaphore for a frame!");
                }
                inFlightFences[i] = _fenceSetup.FenceCreate(_context, true);
            }

            var imagesInflight = new VulkanFence[_context.Swapchain.SwapchainImages!.Length];

            _context.SetupSemaphores(imageAvailableSemaphores, renderFinishedSemaphores);
            _context.SetupFences(inFlightFences, imagesInflight);
        }

        private void DestroySemaphoresAndFences()
        {
            if (_context == null || _context.Swapchain == null)
            {
                return;
            }
            for (int i = 0; i < _context.Swapchain.MaxFramesInFlight; i++)
            {
                _context.Vk.DestroySemaphore(_context.Device.LogicalDevice, _context.ImageAvailableSemaphores![i], _context.Allocator);
                _context.Vk.DestroySemaphore(_context.Device.LogicalDevice, _context.QueueCompleteSemaphores![i], _context.Allocator);
                _fenceSetup.FenceDestroy(_context, _context.InFlightFences![i]);

                _context.ImageAvailableSemaphores![i] = default;
                _context.QueueCompleteSemaphores![i] = default;
                _context.InFlightFences![i] = default;
            }
        }

        private bool RecreateSwapchain()
        {
            if (_context == null || _context.Swapchain == null)
            {
                return false;
            }
            if (_context.RecreatingSwapchain)
            {
                return false;
            }
            if (_cachedFramebufferSize.X == 0 || _cachedFramebufferSize.Y == 0)
            {
                return false;
            }
            _context.SetRecreateSwapchain(true);
            //wait for any operations to finish
            _context.Vk.DeviceWaitIdle(_context.Device.LogicalDevice);

            //clear these out
            for (int cnt = 0; cnt < _context.ImagesInFlight!.Length; cnt++)
            {
                _context.ImagesInFlight[cnt] = default;
            }

            var device = _context.Device;
            device.SwapchainSupport = _deviceSetup.QuerySwapChainSupport(_context.Device.PhysicalDevice, _context);
            _context.SetupDevice(device);
            _deviceSetup.DetectDepthFormat(_context);

            var swapchain = _swapchainSetup.Recreate(_context, _cachedFramebufferSize, _context.Swapchain);
            _context.SetupSwapchain(swapchain);

            _context.SetFramebufferSize(_cachedFramebufferSize, _context.FramebufferSizeGeneration);
            VulkanRenderpass renderPass = _context.MainRenderPass!;
            var rect = renderPass.Rect;
            rect.Offset = new Offset2D(0,0);
            rect.Extent.Width = _context.FramebufferSize.X;
            rect.Extent.Height = _context.FramebufferSize.Y;
            renderPass.Rect = rect;

            _context.SetupMainRenderpass(renderPass);

            VulkanRenderpass uiRenderPass = _context.UiRenderPass!;
            uiRenderPass.Rect = rect;
            _context.SetupUiRenderpass(uiRenderPass);

            //update framebuffer size generation
            _context.SetFramebufferSizeGenerationLastGeneration(_context.FramebufferSizeGeneration);

            for (int cnt = 0; cnt < _context.GraphicsCommandBuffers!.Length; cnt++)
            {
                _context.GraphicsCommandBuffers[cnt] = _commandBufferSetup.CommandBufferFree(_context, _context.Device.GraphicsCommandPool, _context.GraphicsCommandBuffers[cnt]);
            }

            swapchain = DestroyFramebuffers(swapchain);
            _context.SetupSwapchain(swapchain);

            //TODO: something something struct later
            renderPass = _context.MainRenderPass!;
            rect = renderPass.Rect;
            rect.Offset = new Offset2D(0, 0);
            rect.Extent.Width = _context.FramebufferSize.X;
            rect.Extent.Height = _context.FramebufferSize.Y;
            renderPass.Rect = rect;
            _context.SetupMainRenderpass(renderPass);

            uiRenderPass = _context.UiRenderPass!;
            uiRenderPass.Rect = rect;
            _context.SetupUiRenderpass(uiRenderPass);

            _context.SetupSwapchain(RegenerateFramebuffers());

            CreateCommandBuffers();

            // Clear the recreating flag.
            _context.SetRecreateSwapchain(false);

            return true;
        }

        private void CreateBuffers(VulkanContext context)
        {
            MemoryPropertyFlags memPropFlags = MemoryPropertyFlags.DeviceLocalBit;

            // Geometry vertex buffer
            //about 64 MB when complete
            ulong vertexBufferSize = (ulong)sizeof(Vertex3d) * 1024UL * 1024UL;
            var objectVertexUsage = BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit | BufferUsageFlags.TransferSrcBit;

            var objectVertexBuffer = _bufferManager.BufferCreate(context, vertexBufferSize, objectVertexUsage, memPropFlags, true);

            // Geometry index buffer
            ulong indexBufferSize = sizeof(uint) * 1024UL * 1024UL;
            var objectIndexUsage = BufferUsageFlags.IndexBufferBit | BufferUsageFlags.TransferDstBit | BufferUsageFlags.TransferSrcBit;

            var objectIndexBuffer = _bufferManager.BufferCreate(context, indexBufferSize, objectIndexUsage, memPropFlags, true);

            context.SetupBuffers(objectVertexBuffer, objectIndexBuffer);
        }

        private void DestroyBuffers(VulkanContext context)
        {
            var vertBuffer = _bufferManager.BufferDestroy(context, context.ObjectVertexBuffer);
            var indexBuffer = _bufferManager.BufferDestroy(context, context.ObjectIndexBuffer);
            context.SetupBuffers(vertBuffer, indexBuffer);
        }

        private Foxis.Library.Result<ulong> UploadDataRange<T>(VulkanContext context, CommandPool pool, Fence fence, Queue queue, VulkanBuffer buffer, ulong size, Span<T> data)
        {
            //the offset
            var allocateResult = _bufferManager.Allocate(buffer, size);
            if(!allocateResult.Success)
            {
                return allocateResult;
            }

            //Create a host visible staging buffer to upload to. Mark it as the source of the transfer
            var flags = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit;
            var staging = _bufferManager.BufferCreate(context, size, BufferUsageFlags.TransferSrcBit, flags, true);

            //load the data into the staging buffer
            _bufferManager.BufferLoadData(context, staging, 0, size, 0, data);

            //perform the copy from staging to the device local buffer
            _bufferManager.BufferCopyTo(context, pool, fence, queue, staging.Handle, 0, buffer.Handle, allocateResult.Value, size);

            //clean up the staging buffer
            _bufferManager.BufferDestroy(context, staging);

            return allocateResult;
        }

        private void FreeDataRange(VulkanBuffer buffer, ulong offset, ulong size)
        {
            if(_context == null)
            {
                return;
            }
            //update free list with this range being free.
            buffer.Freelist.FreeBlock(size, offset);
        }
    }
}
