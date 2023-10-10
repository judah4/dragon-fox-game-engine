using DragonGameEngine.Core;
using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Rendering.Vulkan.Domain;
using DragonGameEngine.Core.Rendering.Vulkan.Shaders;
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
    public sealed unsafe class VulkanBackendRenderer : IRenderer
    {
        private readonly ILogger _logger;
        private readonly string _applicationName;
        private readonly IWindow _window;

        private VulkanContext? _context;
        private readonly VulkanDeviceSetup _deviceSetup;
        private readonly VulkanSwapchainSetup _swapchainSetup;
        private readonly VulkanImageSetup _imageSetup;
        private readonly VulkanRenderpassSetup _renderpassSetup;
        private readonly VulkanCommandBufferSetup _commandBufferSetup;
        private readonly VulkanFramebufferSetup _framebufferSetup;
        private readonly VulkanFenceSetup _fenceSetup;
        private readonly VulkanShaderSetup _shaderSetup;
        private readonly VulkanObjectShaderSetup _objectShaderSetup;
        private readonly VulkanPipelineSetup _pipelineSetup;
        private readonly VulkanBufferSetup _bufferSetup;

#if DEBUG
        private readonly bool EnableValidationLayers = true; //enable when tools are installed. Add to config
#else
        private readonly bool EnableValidationLayers = false;
#endif

        private readonly string[] _validationLayers = new[]
{
            "VK_LAYER_KHRONOS_validation"
        };

        public VulkanBackendRenderer(string applicationName, IWindow window, ILogger logger)
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
            _bufferSetup = new VulkanBufferSetup(_imageSetup, _commandBufferSetup, logger);

            //shaders
            _shaderSetup = new VulkanShaderSetup();
            _pipelineSetup = new VulkanPipelineSetup(logger);
            _objectShaderSetup = new VulkanObjectShaderSetup(logger, _shaderSetup, _pipelineSetup, _bufferSetup);

        }

        public void Init()
        {
            if (_context != null)
            {
                throw new Exception("Vulkan is already initialized!");
            }

            var vk = Vk.GetApi();

            //if (EnableValidationLayers && !CheckValidationLayerSupport())
            //{
            //    throw new Exception("validation layers requested, but not available!");
            //}

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
                                throw new Exception($"Required Validation Layer is missing: {layer}");
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
            if (vk.CreateInstance(createInfo, allocator, out var instance) != Result.Success)
            {
                throw new Exception("failed to create Vulkan instance!");
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
            _context = new VulkanContext(vk, _window, instance, allocator, debugUtils, debugMessenger, framebufferSize);

            VulkanPlatform.CreateSurface(_context, _logger);

            //Device creation
            _deviceSetup.Create(_context);

            //Swapchain
            _swapchainSetup.Create(_context, _context.FramebufferSize);

            _renderpassSetup.Create(_context, new Rect2D(new Offset2D(0, 0), new Extent2D(_context.FramebufferSize.X, _context.FramebufferSize.Y)),
                System.Drawing.Color.CornflowerBlue, 1.0f, 0);

            //Create frame buffers.
            var swapchain = _context.Swapchain;
            swapchain.Framebuffers = new VulkanFramebuffer[_context.Swapchain.SwapchainImages.Length];
            _context.SetupSwapchain(swapchain);
            swapchain = RegenerateFramebuffers(swapchain, _context.MainRenderPass);
            _context.SetupSwapchain(swapchain); //this all feels real nasty but it works I guess

            //Create command buffers
            CreateCommandBuffers();

            CreateSemaphoresAndFences();

            var objectShader = _objectShaderSetup.ObjectShaderCreate(_context);
            _context.SetupBuiltinShaders(objectShader);

            CreateBuffers(_context);

            //TODO: Temporary test geometrydw

            float trigSize = 5f;

            var verts = new Vertex3d[]
            {
                new Vertex3d(new Vector3D<float>(0, 0.5f, 0) * trigSize),
                new Vertex3d(new Vector3D<float>(-0.5f, -0.5f, 0) * trigSize),
                new Vertex3d(new Vector3D<float>(0.5f, -0.5f, 0) * trigSize),
            };

            // ___
            // | /
            // |/

            var indices = new uint[]
            {
                0,1,2,
            };

            UploadDataRange(_context, _context.Device.GraphicsCommandPool, default, _context.Device.GraphicsQueue, _context.ObjectVertexBuffer, 0, (ulong)(sizeof(Vertex3d) * verts.LongLength), verts.AsSpan());
            UploadDataRange(_context, _context.Device.GraphicsCommandPool, default, _context.Device.GraphicsQueue, _context.ObjectIndexBuffer, 0, (ulong)(sizeof(uint) * indices.LongLength), indices.AsSpan());
            //todo: end test code.

            _logger.LogInformation($"Vulkan initialized.");
        }

        public void Shutdown()
        {
            if (_context == null)
                return;

            //wait until the device is idle again
            _context.Vk.DeviceWaitIdle(_context.Device.LogicalDevice);

            DestroyBuffers(_context);

            _objectShaderSetup.ObjectShaderDestroy(_context, _context.ObjectShader);

            DestroySemaphoresAndFences();

            CleanUpCommandBuffers();

            var swapchain = DestroyFramebuffers(_context.Swapchain);
            _context.SetupSwapchain(swapchain); //this all feels real nasty but it works I guess

            _renderpassSetup.Destory(_context, _context.MainRenderPass);

            _swapchainSetup.Destroy(_context, _context.Swapchain);

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
            _context.SetFramebufferSize(size, _context.FramebufferSizeGeneration + 1);

            _logger.LogDebug("Vulkan Backend Renderer resized {size}, generation {framebufferSizeGeneration}", size, _context.FramebufferSizeGeneration);
        }

        public bool BeginFrame(double deltaTime)
        {
            if (_context == null)
            {
                return false;
            }
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
            var imageIndex = _swapchainSetup.AquireNextImageIndex(_context, _context.Swapchain, ulong.MaxValue, _context.ImageAvailableSemaphores![_context.CurrentFrame], default);
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

            var renderPass = _context.MainRenderPass;
            renderPass.Rect.Extent = scissor.Extent; //hacky but sets it at least
            _context.SetupMainRenderpass(renderPass);

            commandBuffer = _renderpassSetup.BeginRenderpass(_context, commandBuffer, _context.MainRenderPass, _context.Swapchain.Framebuffers[_context.ImageIndex].Framebuffer);
            _context.GraphicsCommandBuffers[_context.ImageIndex] = commandBuffer;
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
            //End renderpass
            commandBuffer = _renderpassSetup.EndRenderpass(_context, commandBuffer, _context.MainRenderPass);

            commandBuffer = _commandBufferSetup.CommandBufferEnd(_context, commandBuffer);
            _context.GraphicsCommandBuffers[_context.ImageIndex] = commandBuffer;

            //make sure the previous frame is not using this image
            if (_context.ImagesInFlight![_context.ImageIndex] != default)
            {
                _fenceSetup.FenceWait(_context, _context.InFlightFences![_context.CurrentFrame], ulong.MaxValue);
            }

            //mark in use
            fixed (VulkanFence* fencePtr = &_context.InFlightFences![_context.CurrentFrame])
            {
                _context.ImagesInFlight[_context.ImageIndex] = fencePtr;
            }

            _context.InFlightFences[_context.CurrentFrame] = _fenceSetup.FenceReset(_context, _context.InFlightFences[_context.CurrentFrame]);

            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &commandBuffer.Handle,
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

            _swapchainSetup.Present(_context, _context.Swapchain, _context.Device.GraphicsQueue, _context.Device.PresentQueue, signalSemaphores, _context.ImageIndex);

            return;
        }

        public void UpdateGlobalState(Matrix4X4<float> projection, Matrix4X4<float> view, Vector3D<float> viewPosition, System.Drawing.Color ambientColor, int mode)
        {
            var commandBuffer = _context!.GraphicsCommandBuffers![_context.ImageIndex];

            _objectShaderSetup.ObjectShaderUse(_context, _context.ObjectShader);

            //update the view and projection
            var objectShader = _context.ObjectShader;
            objectShader.GlobalUbo.Projection = projection;
            objectShader.GlobalUbo.View = view;
            //TODO: other ubo properties

            _context.SetupBuiltinShaders(objectShader);

            _objectShaderSetup.UpdateGlobalState(_context, _context.ObjectShader);

            //TODO: Test code to draw

            var vertexBuffers = new Buffer[] { _context.ObjectVertexBuffer.Handle };
            //bind vertex buffer at offset
            var offsets = new ulong[] { 0 };

            fixed (ulong* offsetsPtr = offsets)
            fixed (Buffer* vertexBuffersPtr = vertexBuffers)
            {
                _context.Vk.CmdBindVertexBuffers(commandBuffer.Handle, 0, 1, vertexBuffersPtr, offsetsPtr);
            }

            //bind index buffer at offset
            _context.Vk.CmdBindIndexBuffer(commandBuffer.Handle, _context.ObjectIndexBuffer.Handle, 0, IndexType.Uint32);

            //issue the draw. hardcode to 3 indices for now
            _context.Vk.CmdDrawIndexed(commandBuffer.Handle, 3U, 1, 0, 0, 0);
            //drawCall++
            //end temp test code
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

            if (debugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out debugMessenger) != Result.Success)
            {
                throw new Exception("failed to set up debug messenger!");
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
            var commandBuffers = _context!.GraphicsCommandBuffers;
            if (commandBuffers == null || commandBuffers.Length != _context.Swapchain.SwapchainImages.Length)
            {
                //destroy previous if needed?
                commandBuffers = new VulkanCommandBuffer[_context.Swapchain.SwapchainImages.Length];
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

        private VulkanSwapchain RegenerateFramebuffers(VulkanSwapchain swapchain, VulkanRenderpass renderpass)
        {
            if (_context == null)
            {
                throw new Exception("Context is not set up. Was RegenerateFramebuffers called before Vulkan is initialized?");
            }
            if (swapchain.ImageViews == null)
            {
                _logger.LogWarning("Image views are emtpy for regenerating frame buffers. Why?");
                return swapchain;
            }

            for (int cnt = 0; cnt < swapchain.ImageViews.Length; cnt++)
            {
                var attachments = new[]
                {
                    swapchain.ImageViews[cnt],
                    swapchain.DepthAttachment.ImageView,
                };

                swapchain.Framebuffers[cnt] = _framebufferSetup.FramebufferCreate(_context!, renderpass, _context!.FramebufferSize, attachments);
            }
            return swapchain;
        }

        private VulkanSwapchain DestroyFramebuffers(VulkanSwapchain swapchain)
        {
            for (int cnt = 0; cnt < swapchain.Framebuffers.Length; cnt++)
            {
                swapchain.Framebuffers[cnt] = _framebufferSetup.FramebufferDestroy(_context!, swapchain.Framebuffers[cnt]);
            }
            return swapchain;
        }

        private void CreateSemaphoresAndFences()
        {
            if (_context == null)
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
                if (_context.Vk.CreateSemaphore(_context.Device.LogicalDevice, semaphoreInfo, _context.Allocator, out imageAvailableSemaphores[i]) != Result.Success ||
                    _context.Vk.CreateSemaphore(_context.Device.LogicalDevice, semaphoreInfo, _context.Allocator, out renderFinishedSemaphores[i]) != Result.Success)
                {
                    throw new Exception("Failed to create synchronization objects for a frame!");
                }
                inFlightFences[i] = _fenceSetup.FenceCreate(_context, true);
            }

            var imagesInflight = new VulkanFence*[_context.Swapchain.SwapchainImages.Length];

            _context.SetupSemaphores(imageAvailableSemaphores, renderFinishedSemaphores);
            _context.SetupFences(inFlightFences, imagesInflight);
        }

        private void DestroySemaphoresAndFences()
        {
            if (_context == null)
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
            if (_context == null)
            {
                return false;
            }
            if (_context.RecreatingSwapchain)
            {
                return false;
            }
            if (_context.FramebufferSize.X == 0 || _context.FramebufferSize.Y == 0)
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

            var swapchain = _swapchainSetup.Recreate(_context, _context.FramebufferSize, _context.Swapchain);
            _context.SetupSwapchain(swapchain);

            var renderPass = _context.MainRenderPass;
            renderPass.Rect.Extent.Width = _context.FramebufferSize.X;
            renderPass.Rect.Extent.Height = _context.FramebufferSize.Y;
            _context.SetupMainRenderpass(renderPass);

            //update framebuffer size generation
            _context.SetFramebufferSizeGenerationLastGeneration(_context.FramebufferSizeGeneration);

            for (int cnt = 0; cnt < _context.GraphicsCommandBuffers!.Length; cnt++)
            {
                _context.GraphicsCommandBuffers[cnt] = _commandBufferSetup.CommandBufferFree(_context, _context.Device.GraphicsCommandPool, _context.GraphicsCommandBuffers[cnt]);
            }

            DestroyFramebuffers(swapchain);

            //something something struct later
            renderPass = _context.MainRenderPass;
            renderPass.Rect.Offset.X = 0;
            renderPass.Rect.Offset.Y = 0;
            renderPass.Rect.Extent.Width = _context.FramebufferSize.X;
            renderPass.Rect.Extent.Height = _context.FramebufferSize.Y;
            _context.SetupMainRenderpass(renderPass);

            _context.SetupSwapchain(RegenerateFramebuffers(_context.Swapchain, _context.MainRenderPass));

            CreateCommandBuffers();

            // Clear the recreating flag.
            _context.SetRecreateSwapchain(false);

            return true;
        }

        private void CreateBuffers(VulkanContext context)
        {
            MemoryPropertyFlags memPropFlags = MemoryPropertyFlags.DeviceLocalBit;

            //about 64 MB when complete
            ulong vertexBufferSize = (ulong)sizeof(Vertex3d) * 1024UL * 1024UL;
            var objectVertexUsage = BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit | BufferUsageFlags.TransferSrcBit;

            var objectVertexBuffer = _bufferSetup.BufferCreate(context, vertexBufferSize, objectVertexUsage, memPropFlags, true);

            ulong indexBufferSize = sizeof(uint) * 1024UL * 1024UL;
            var objectIndexUsage = BufferUsageFlags.IndexBufferBit | BufferUsageFlags.TransferDstBit | BufferUsageFlags.TransferSrcBit;

            var objectIndexBuffer = _bufferSetup.BufferCreate(context, indexBufferSize, objectIndexUsage, memPropFlags, true);

            context.SetupBuffers(objectVertexBuffer, objectIndexBuffer);
        }

        private void DestroyBuffers(VulkanContext context)
        {
            var vertBuffer = _bufferSetup.BufferDestroy(context, context.ObjectVertexBuffer);
            var indexBuffer = _bufferSetup.BufferDestroy(context, context.ObjectIndexBuffer);
            context.SetupBuffers(vertBuffer, indexBuffer);
            context.SetupBufferOffsets(0, 0);
        }

        private void UploadDataRange<T>(VulkanContext context, CommandPool pool, Fence fence, Queue queue, VulkanBuffer buffer, ulong offset, ulong size, Span<T> data)
        {
            //Create a host visible staging buffer to upload to. Mark it as the source of the transfer
            var flags = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit;
            var staging = _bufferSetup.BufferCreate(context, size, BufferUsageFlags.TransferSrcBit, flags, true);

            //load the data into the staging buffer
            _bufferSetup.BufferLoadData(context, staging, 0, size, 0, data);

            //perform the copy from staging to the device local buffer
            _bufferSetup.BufferCopyTo(context, pool, fence, queue, staging.Handle, 0, buffer.Handle, offset, size);

            //clean up the staging buffer
            _bufferSetup.BufferDestroy(context, staging);
        }
    }
}
