using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;
using Microsoft.Extensions.Logging;
using DragonFoxGameEngine.Core.Systems;
using Svelto.ECS.Schedulers;
using Silk.NET.Input;
using DragonFoxGameEngine.Core.Vulkan;
using SixLabors.ImageSharp.Advanced;

namespace DragonFoxGameEngine.Core
{

    struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public bool IsComplete()
        {
            return GraphicsFamily.HasValue && PresentFamily.HasValue;
        }
    }

    struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats;
        public PresentModeKHR[] PresentModes;
    }

    struct Vertex
    {
        public Vector3D<float> pos;
        public Vector3D<float> color;
        public Vector2D<float> textCoord;

        public static VertexInputBindingDescription GetBindingDescription()
        {
            VertexInputBindingDescription bindingDescription = new()
            {
                Binding = 0,
                Stride = (uint)Unsafe.SizeOf<Vertex>(),
                InputRate = VertexInputRate.Vertex,
            };

            return bindingDescription;
        }

        public static VertexInputAttributeDescription[] GetAttributeDescriptions()
        {
            var attributeDescriptions = new[]
            {
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(pos)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(color)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(textCoord)),
            }
        };

            return attributeDescriptions;
        }
    }

    struct UniformBufferObject
    {
        public Matrix4X4<float> model;
        public Matrix4X4<float> view;
        public Matrix4X4<float> proj;
    }

    public unsafe class HelloTriangleApplication
    {

        const int WIDTH = 800;
        const int HEIGHT = 600;

        const int MAX_FRAMES_IN_FLIGHT = 2;

        const string GAME_ENGINE_NAME = "Dragon Fox Game Engine";
        const string DEFAULT_WINDOW_TITLE = "Project Dragon Fox Game Engine";

        private readonly ILogger _logger;
        private readonly SystemEnginesGroup _ecsSystemEnginesGroup;
        private readonly SimpleEntitiesSubmissionScheduler _entitiesSubmissionScheduler;
        private IKeyboard? _primaryKeyboard;


#if DEBUG
        bool EnableValidationLayers = false; //enable when tools are installed. Add to config
#else
        bool EnableValidationLayers = false;
#endif

        private readonly string[] validationLayers = new[]
        {
            "VK_LAYER_KHRONOS_validation"
        };

        private readonly string[] deviceExtensions = new[]
        {
            KhrSwapchain.ExtensionName
        };

        //Setup the camera's location, directions, and movement speed
        private Vector3D<float> CameraPosition = new Vector3D<float>(0.0f, 1.0f, 3.0f);
        private Vector3D<float> CameraFront = new Vector3D<float>(0.0f, 0.0f, -1.0f);
        private Vector3D<float> CameraUp = Vector3D<float>.UnitY;
        private Vector3D<float> CameraDirection = Vector3D<float>.Zero;
        private float CameraYaw = -90f;
        private float CameraPitch = 0f;
        private float CameraZoom = 45f;

        //Used to track change in mouse movement to allow for moving of the Camera
        private Vector2D<float> LastMousePosition;

        private IWindow? window;
        private Vk? vk;

        private Instance instance;

        private ExtDebugUtils? debugUtils;
        private DebugUtilsMessengerEXT debugMessenger;
        private KhrSurface? khrSurface;
        private SurfaceKHR surface;

        private PhysicalDevice _physicalDevice;
        private Device _device;

        private Queue graphicsQueue;
        private Queue presentQueue;

        private KhrSwapchain? khrSwapChain;
        private SwapchainKHR swapChain;
        private Image[]? swapChainImages;
        private Format swapChainImageFormat;
        private Extent2D swapChainExtent;
        private ImageView[]? swapChainImageViews;
        private Framebuffer[]? _swapChainFramebuffers;
        private Framebuffer[]? _uiFramebuffers;

        private RenderPass _renderPass;
        private RenderPass _uiRenderPass;

        private DescriptorSetLayout _descriptorSetLayout;
        private VulkanPipeline? _vulkanPipeline;
        private VulkanPipelineData _graphicsWorldPipelineData;
        private VulkanPipelineData _uiPipelineData;


        private CommandPool commandPool;

        private Image depthImage;
        private DeviceMemory depthImageMemory;
        private ImageView depthImageView;

        private Image _textureImage;
        private DeviceMemory _textureImageMemory;
        private ImageView _textureImageView;
        private Sampler _textureSampler;

        private Silk.NET.Vulkan.Buffer vertexBuffer;
        private DeviceMemory vertexBufferMemory;
        private Buffer indexBuffer;
        private DeviceMemory indexBufferMemory;

        private Buffer[]? uniformBuffers;
        private DeviceMemory[]? uniformBuffersMemory;

        private DescriptorPool descriptorPool;
        private DescriptorSet[]? descriptorSets;

        private CommandBuffer[]? _commandBuffers;

        private Semaphore[]? imageAvailableSemaphores;
        private Semaphore[]? renderFinishedSemaphores;
        private Fence[]? inFlightFences;
        private Fence[]? imagesInFlight;
        private int currentFrame = 0;

        private bool frameBufferResized = false;

        private Vertex[] vertices = new Vertex[]
        {
            new Vertex { pos = new Vector3D<float>(-0.5f,-0.5f, 0.5f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f), textCoord = new Vector2D<float>(1.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(0.5f,-0.5f, 0.5f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f), textCoord = new Vector2D<float>(0.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(0.5f,0.5f, 0.5f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f), textCoord = new Vector2D<float>(0.0f, 1.0f) },
            new Vertex { pos = new Vector3D<float>(-0.5f,0.5f, 0.5f), color = new Vector3D<float>(1.0f, 1.0f, 1.0f), textCoord = new Vector2D<float>(1.0f, 1.0f) },

            new Vertex { pos = new Vector3D<float>(-0.5f,-0.5f, -0.5f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f), textCoord = new Vector2D<float>(1.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(0.5f,-0.5f, -0.5f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f), textCoord = new Vector2D<float>(0.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(0.5f,0.5f, -0.5f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f), textCoord = new Vector2D<float>(0.0f, 1.0f) },
            new Vertex { pos = new Vector3D<float>(-0.5f,0.5f, -0.5f), color = new Vector3D<float>(1.0f, 1.0f, 1.0f), textCoord = new Vector2D<float>(1.0f, 1.0f) },

            new Vertex { pos = new Vector3D<float>(-0.5f,-0.5f, -0.5f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f), textCoord = new Vector2D<float>(1.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(-0.5f,-0.5f, 0.5f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f), textCoord = new Vector2D<float>(0.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(-0.5f,0.5f, 0.5f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f), textCoord = new Vector2D<float>(0.0f, 1.0f) },
            new Vertex { pos = new Vector3D<float>(-0.5f,0.5f, -0.5f), color = new Vector3D<float>(1.0f, 1.0f, 1.0f), textCoord = new Vector2D<float>(1.0f, 1.0f) },

            new Vertex { pos = new Vector3D<float>(0.5f,-0.5f, -0.5f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f), textCoord = new Vector2D<float>(1.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(0.5f,-0.5f, 0.5f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f), textCoord = new Vector2D<float>(0.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(0.5f,0.5f, 0.5f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f), textCoord = new Vector2D<float>(0.0f, 1.0f) },
            new Vertex { pos = new Vector3D<float>(0.5f,0.5f, -0.5f), color = new Vector3D<float>(1.0f, 1.0f, 1.0f), textCoord = new Vector2D<float>(1.0f, 1.0f) },

            new Vertex { pos = new Vector3D<float>(-0.5f,-0.5f, -0.5f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f), textCoord = new Vector2D<float>(1.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(0.5f,-0.5f, -0.5f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f), textCoord = new Vector2D<float>(0.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(0.5f,-0.5f, 0.5f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f), textCoord = new Vector2D<float>(0.0f, 1.0f) },
            new Vertex { pos = new Vector3D<float>(-0.5f,-0.5f, 0.5f), color = new Vector3D<float>(1.0f, 1.0f, 1.0f), textCoord = new Vector2D<float>(1.0f, 1.0f) },

            new Vertex { pos = new Vector3D<float>(-0.5f,0.5f, -0.5f), color = new Vector3D<float>(1.0f, 0.0f, 0.0f), textCoord = new Vector2D<float>(1.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(0.5f,0.5f, -0.5f), color = new Vector3D<float>(0.0f, 1.0f, 0.0f), textCoord = new Vector2D<float>(0.0f, 0.0f) },
            new Vertex { pos = new Vector3D<float>(0.5f,0.5f, 0.5f), color = new Vector3D<float>(0.0f, 0.0f, 1.0f), textCoord = new Vector2D<float>(0.0f, 1.0f) },
            new Vertex { pos = new Vector3D<float>(-0.5f,0.5f, 0.5f), color = new Vector3D<float>(1.0f, 1.0f, 1.0f), textCoord = new Vector2D<float>(1.0f, 1.0f) },
        };

        private ushort[] indices = new ushort[]
        {
            0, 1, 2, 2, 3, 0,
            4, 7, 6, 6, 5, 4,
            8, 9, 10, 10, 11, 8,
            12, 15, 14, 14, 13, 12,
            16, 17, 18, 18, 19, 16,
            20, 23, 22, 22, 21, 20,
        };

        public HelloTriangleApplication(ILogger logger, SystemEnginesGroup ecsSystemEnginesGroup, Svelto.ECS.Schedulers.SimpleEntitiesSubmissionScheduler entitiesSubmissionScheduler)
        {
            _logger = logger;
            _ecsSystemEnginesGroup = ecsSystemEnginesGroup;
            _entitiesSubmissionScheduler = entitiesSubmissionScheduler;
        }

        public void Run()
        {
            InitWindow();
            InitVulkan();
            MainLoop();
            CleanUp();
        }

        private void InitWindow()
        {

            //Create a window.
            var options = WindowOptions.DefaultVulkan with
            {
                //IsVisible = false, //use IsVisible for setting up headless mode later
                Size = new Vector2D<int>(WIDTH, HEIGHT),
                Title = DEFAULT_WINDOW_TITLE,
            };

            window = Window.Create(options);
            window.Load += OnLoad;
            window.Initialize();

            try
            {
                using var image = SixLabors.ImageSharp.Image.Load<Rgba32>("favicon.png");
                var memoryGroup = image.GetPixelMemoryGroup();
                Memory<byte> array = new byte[memoryGroup.TotalLength * sizeof(Rgba32)];
                var block = MemoryMarshal.Cast<byte, Rgba32>(array.Span);
                foreach (var memory in memoryGroup)
                {
                    memory.Span.CopyTo(block);
                    block = block.Slice(memory.Length);
                }

                var icon = new RawImage(image.Width, image.Height, array);
                window!.SetWindowIcon(ref icon);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }


            if (window.VkSurface is null)
            {
                throw new Exception("Windowing platform doesn't support Vulkan.");
            }

            window.Resize += FramebufferResizeCallback;

            window!.Update += OnUpdate;
            window!.Render += DrawFrame;
        }

        private void OnLoad()
        {
            IInputContext input = window!.CreateInput();
            _primaryKeyboard = input.Keyboards.First();
            if (_primaryKeyboard != null)
            {
                _primaryKeyboard.KeyDown += KeyDown;
            }
            for (int i = 0; i < input.Mice.Count; i++)
            {
                input.Mice[i].Cursor.CursorMode = CursorMode.Raw;
                input.Mice[i].MouseMove += OnMouseMove;
                input.Mice[i].Scroll += OnMouseWheel;
            }

            
        }

        private void FramebufferResizeCallback(Vector2D<int> size)
        {
            frameBufferResized = true;
        }

        private void InitVulkan()
        {
            CreateInstance();
            SetupDebugMessenger();
            CreateSurface();
            PickPhysicalDevice();
            CreateLogicalDevice();
            CreateSwapChain();
            CreateImageViews();
            CreateRenderPasses();
            CreateDescriptorSetLayout();
            CreateGraphicsPipeline();
            CreateCommandPool();
            CreateDepthResources();
            CreateFramebuffers();
            CreateTextureImage();
            CreateTextureImageView();
            CreateTextureSampler();
            CreateVertexBuffer();
            CreateIndexBuffer();
            CreateUniformBuffers();
            CreateDescriptorPool();
            CreateDescriptorSets();
            CreateCommandBuffers();
            CreateSyncObjects();
        }

        private void MainLoop()
        {
            window!.Run();
            vk!.DeviceWaitIdle(_device);
        }

        private void OnUpdate(double deltaTime)
        {
            _entitiesSubmissionScheduler.SubmitEntities();
            _ecsSystemEnginesGroup.Step(deltaTime);

            var moveSpeed = 2.5f * (float)deltaTime;

            if (_primaryKeyboard!.IsKeyPressed(Key.W))
            {
                //Move forwards
                CameraPosition += moveSpeed * CameraFront;
            }
            if (_primaryKeyboard.IsKeyPressed(Key.S))
            {
                //Move backwards
                CameraPosition -= moveSpeed * CameraFront;
            }
            if (_primaryKeyboard.IsKeyPressed(Key.A))
            {
                //Move left
                CameraPosition -= Vector3D.Normalize(Vector3D.Cross(CameraFront, CameraUp)) * moveSpeed;
            }
            if (_primaryKeyboard.IsKeyPressed(Key.D))
            {
                //Move right
                CameraPosition += Vector3D.Normalize(Vector3D.Cross(CameraFront, CameraUp)) * moveSpeed;
            }
        }

        private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
        {
            var mousePos = new Vector2D<float>(position.X, position.Y);
            var lookSensitivity = 0.1f;
            if (LastMousePosition == default) 
            { 
                LastMousePosition = mousePos;
            }
            else
            {
                var xOffset = (position.X - LastMousePosition.X) * lookSensitivity;
                var yOffset = (position.Y - LastMousePosition.Y) * lookSensitivity;
                LastMousePosition = mousePos;

                CameraYaw += xOffset;
                CameraPitch -= yOffset;

                //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
                CameraPitch = Math.Clamp(CameraPitch, -89.0f, 89.0f);

                CameraDirection.X = MathF.Cos(Scalar.DegreesToRadians(CameraYaw)) * MathF.Cos(Scalar.DegreesToRadians(CameraPitch));
                CameraDirection.Y = MathF.Sin(Scalar.DegreesToRadians(CameraPitch));
                CameraDirection.Z = MathF.Sin(Scalar.DegreesToRadians(CameraYaw)) * MathF.Cos(Scalar.DegreesToRadians(CameraPitch));
                //_logger.LogDebug($"{CameraFront} {CameraDirection}");
                CameraFront = Vector3D.Normalize(CameraDirection);
            }
        }

        private void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
        {
            //We don't want to be able to zoom in too close or too far away so clamp to these values
            CameraZoom = Math.Clamp(CameraZoom - scrollWheel.Y, 1.0f, 45f);
        }

        private void KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            if (key == Key.Escape)
            {
                window!.Close();
            }
        }

        private void CleanUpSwapChain()
        {
            vk!.DestroyImageView(_device, depthImageView, null);
            vk!.DestroyImage(_device, depthImage, null);
            vk!.FreeMemory(_device, depthImageMemory, null);

            foreach (var framebuffer in _swapChainFramebuffers!)
            {
                vk!.DestroyFramebuffer(_device, framebuffer, null);
            }
            foreach (var framebuffer in _uiFramebuffers!)
            {
                vk!.DestroyFramebuffer(_device, framebuffer, null);
            }

            fixed (CommandBuffer* commandBuffersPtr = _commandBuffers)
            {
                vk!.FreeCommandBuffers(_device, commandPool, (uint)_commandBuffers!.Length, commandBuffersPtr);
            }

            _vulkanPipeline!.CleanUpPipeline(vk, _device, _graphicsWorldPipelineData);
            _vulkanPipeline!.CleanUpPipeline(vk, _device, _uiPipelineData);

            vk!.DestroyRenderPass(_device, _renderPass, null);
            vk!.DestroyRenderPass(_device, _uiRenderPass, null);

            foreach (var imageView in swapChainImageViews!)
            {
                vk!.DestroyImageView(_device, imageView, null);
            }

            khrSwapChain!.DestroySwapchain(_device, swapChain, null);

            for (int i = 0; i < swapChainImages!.Length; i++)
            {
                vk!.DestroyBuffer(_device, uniformBuffers![i], null);
                vk!.FreeMemory(_device, uniformBuffersMemory![i], null);
            }

            vk!.DestroyDescriptorPool(_device, descriptorPool, null);
        }

        private void CleanUp()
        {
            CleanUpSwapChain();

            vk!.DestroySampler(_device, _textureSampler, null);
            vk!.DestroyImageView(_device, _textureImageView, null);

            vk!.DestroyImage(_device, _textureImage, null);
            vk!.FreeMemory(_device, _textureImageMemory, null);

            vk!.DestroyDescriptorSetLayout(_device, _descriptorSetLayout, null);

            vk!.DestroyBuffer(_device, indexBuffer, null);
            vk!.FreeMemory(_device, indexBufferMemory, null);

            vk!.DestroyBuffer(_device, vertexBuffer, null);
            vk!.FreeMemory(_device, vertexBufferMemory, null);

            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                vk!.DestroySemaphore(_device, renderFinishedSemaphores![i], null);
                vk!.DestroySemaphore(_device, imageAvailableSemaphores![i], null);
                vk!.DestroyFence(_device, inFlightFences![i], null);
            }

            vk!.DestroyCommandPool(_device, commandPool, null);

            vk!.DestroyDevice(_device, null);

            if (EnableValidationLayers)
            {
                //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
                debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
            }

            khrSurface!.DestroySurface(instance, surface, null);
            vk!.DestroyInstance(instance, null);
            vk!.Dispose();

            window?.Dispose();
        }

        private void RecreateSwapChain()
        {
            Vector2D<int> framebufferSize = window!.FramebufferSize;

            while (framebufferSize.X == 0 || framebufferSize.Y == 0)
            {
                framebufferSize = window.FramebufferSize;
                window.DoEvents();
            }

            vk!.DeviceWaitIdle(_device);

            CleanUpSwapChain();

            CreateSwapChain();
            CreateImageViews();
            CreateRenderPasses();
            CreateGraphicsPipeline();
            CreateDepthResources();
            CreateFramebuffers();
            CreateUniformBuffers();
            CreateDescriptorPool();
            CreateDescriptorSets();
            CreateCommandBuffers();

            imagesInFlight = new Fence[swapChainImages!.Length];
        }

        private void CreateInstance()
        {
            vk = Vk.GetApi();

            if (EnableValidationLayers && !CheckValidationLayerSupport())
            {
                throw new Exception("validation layers requested, but not available!");
            }
            var gameVersion = ApplicationInfo.GameVersion;
            var engineVersion = ApplicationInfo.EngineVersion;
            Silk.NET.Vulkan.ApplicationInfo appInfo = new()
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(DEFAULT_WINDOW_TITLE),
                ApplicationVersion = new Version32((uint)gameVersion.Major, (uint)gameVersion.Minor, (uint)gameVersion.Revision),
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi(GAME_ENGINE_NAME),
                EngineVersion = new Version32((uint)engineVersion.Major, (uint)engineVersion.Minor, (uint)engineVersion.Revision),
                ApiVersion = Vk.Version12
            };

            InstanceCreateInfo createInfo = new()
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo
            };

            var extensions = GetRequiredExtensions();
            createInfo.EnabledExtensionCount = (uint)extensions.Length;
            createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions); ;

            if (EnableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint)validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);

                DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
                PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
                createInfo.PNext = &debugCreateInfo;
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
                createInfo.PNext = null;
            }

            if (vk.CreateInstance(createInfo, null, out instance) != Result.Success)
            {
                throw new Exception("failed to create instance!");
            }

            Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
            Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

            if (EnableValidationLayers)
            {
                SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
            }
        }

        private void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
        {
            createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
            createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
            createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
            createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
        }

        private void SetupDebugMessenger()
        {
            if (!EnableValidationLayers) return;

            //TryGetInstanceExtension equivilant to method CreateDebugUtilsMessengerEXT from original tutorial.
            if (!vk!.TryGetInstanceExtension(instance, out debugUtils)) return;

            DebugUtilsMessengerCreateInfoEXT createInfo = new();
            PopulateDebugMessengerCreateInfo(ref createInfo);

            if (debugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out debugMessenger) != Result.Success)
            {
                throw new Exception("failed to set up debug messenger!");
            }
        }

        private void CreateSurface()
        {
            if (!vk!.TryGetInstanceExtension<KhrSurface>(instance, out khrSurface))
            {
                throw new NotSupportedException("KHR_surface extension not found.");
            }

            surface = window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
        }

        private void PickPhysicalDevice()
        {
            uint devicedCount = 0;
            vk!.EnumeratePhysicalDevices(instance, ref devicedCount, null);

            if (devicedCount == 0)
            {
                throw new Exception("failed to find GPUs with Vulkan support!");
            }

            var devices = new PhysicalDevice[devicedCount];
            fixed (PhysicalDevice* devicesPtr = devices)
            {
                vk!.EnumeratePhysicalDevices(instance, ref devicedCount, devicesPtr);
            }

            foreach (var device in devices)
            {
                if (IsDeviceSuitable(device))
                {
                    _physicalDevice = device;
                    break;
                }
            }

            if (_physicalDevice.Handle == 0)
            {
                throw new Exception("failed to find a suitable GPU!");
            }

            var properties = vk!.GetPhysicalDeviceProperties(_physicalDevice);
            _logger.LogInformation($"Device Type: {properties.DeviceType.ToString()}");
        }

        private void CreateLogicalDevice()
        {
            var indices = FindQueueFamilies(_physicalDevice);

            var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };
            uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

            using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
            var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

            float queuePriority = 1.0f;
            for (int i = 0; i < uniqueQueueFamilies.Length; i++)
            {
                queueCreateInfos[i] = new()
                {
                    SType = StructureType.DeviceQueueCreateInfo,
                    QueueFamilyIndex = uniqueQueueFamilies[i],
                    QueueCount = 1,
                    PQueuePriorities = &queuePriority
                };
            }

            PhysicalDeviceFeatures deviceFeatures = new()
            {
                SamplerAnisotropy = true,
            };


            DeviceCreateInfo createInfo = new()
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
                PQueueCreateInfos = queueCreateInfos,

                PEnabledFeatures = &deviceFeatures,

                EnabledExtensionCount = (uint)deviceExtensions.Length,
                PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions)
            };

            if (EnableValidationLayers)
            {
                createInfo.EnabledLayerCount = (uint)validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
            }
            else
            {
                createInfo.EnabledLayerCount = 0;
            }

            if (vk!.CreateDevice(_physicalDevice, in createInfo, null, out _device) != Result.Success)
            {
                throw new Exception("failed to create logical device!");
            }

            vk!.GetDeviceQueue(_device, indices.GraphicsFamily!.Value, 0, out graphicsQueue);
            vk!.GetDeviceQueue(_device, indices.PresentFamily!.Value, 0, out presentQueue);

            if (EnableValidationLayers)
            {
                SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
            }

            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        }

        private void CreateSwapChain()
        {
            var swapChainSupport = QuerySwapChainSupport(_physicalDevice);

            var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
            var presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
            var extent = ChooseSwapExtent(swapChainSupport.Capabilities);

            var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            SwapchainCreateInfoKHR creatInfo = new()
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = surface,

                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = extent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            };

            var indices = FindQueueFamilies(_physicalDevice);
            var queueFamilyIndices = stackalloc[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

            if (indices.GraphicsFamily != indices.PresentFamily)
            {
                creatInfo = creatInfo with
                {
                    ImageSharingMode = SharingMode.Concurrent,
                    QueueFamilyIndexCount = 2,
                    PQueueFamilyIndices = queueFamilyIndices,
                };
            }
            else
            {
                creatInfo.ImageSharingMode = SharingMode.Exclusive;
            }

            creatInfo = creatInfo with
            {
                PreTransform = swapChainSupport.Capabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = presentMode,
                Clipped = true,
            };

            if (khrSwapChain is null)
            {
                if (!vk!.TryGetDeviceExtension(instance, _device, out khrSwapChain))
                {
                    throw new NotSupportedException("VK_KHR_swapchain extension not found.");
                }
            }

            if (khrSwapChain!.CreateSwapchain(_device, creatInfo, null, out swapChain) != Result.Success)
            {
                throw new Exception("failed to create swap chain!");
            }

            khrSwapChain.GetSwapchainImages(_device, swapChain, ref imageCount, null);
            swapChainImages = new Image[imageCount];
            fixed (Image* swapChainImagesPtr = swapChainImages)
            {
                khrSwapChain.GetSwapchainImages(_device, swapChain, ref imageCount, swapChainImagesPtr);
            }

            swapChainImageFormat = surfaceFormat.Format;
            swapChainExtent = extent;
        }

        private void CreateImageViews()
        {
            swapChainImageViews = new ImageView[swapChainImages!.Length];

            for (int i = 0; i < swapChainImages.Length; i++)
            {

                swapChainImageViews[i] = CreateImageView(swapChainImages[i], swapChainImageFormat, ImageAspectFlags.ColorBit);
            }
        }

        private void CreateRenderPasses()
        {
            _renderPass = CreateWorldRenderPass();
            _uiRenderPass = CreateUiRenderPass();
        }

        private RenderPass CreateWorldRenderPass()
        {
            AttachmentDescription colorAttachment = new()
            {
                Format = swapChainImageFormat,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr,
            };

            AttachmentDescription depthAttachment = new()
            {
                Format = FindDepthFormat(),
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.DontCare,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            };

            //AttachmentDescription uiAttachment = new()
            //{
            //    Format = swapChainImageFormat, //seems to be this?
            //    Samples = SampleCountFlags.Count1Bit,
            //    LoadOp = AttachmentLoadOp.Load,
            //    StoreOp = AttachmentStoreOp.DontCare,
            //    StencilLoadOp = AttachmentLoadOp.DontCare,
            //    StencilStoreOp = AttachmentStoreOp.DontCare,
            //    InitialLayout = ImageLayout.ColorAttachmentOptimal,
            //    FinalLayout = ImageLayout.ColorAttachmentOptimal,
            //};

            AttachmentReference colorAttachmentRef = new()
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal,
            };

            AttachmentReference depthAttachmentRef = new()
            {
                Attachment = 1,
                Layout = ImageLayout.DepthStencilAttachmentOptimal,
            };

            SubpassDescription subpass = new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef,
                PDepthStencilAttachment = &depthAttachmentRef,
            };

            SubpassDependency dependency = new()
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
            };

            var attachments = new[] { colorAttachment, depthAttachment };

            fixed (AttachmentDescription* attachmentsPtr = attachments)
            {
                RenderPassCreateInfo renderPassInfo = new()
                {
                    SType = StructureType.RenderPassCreateInfo,
                    AttachmentCount = (uint)attachments.Length,
                    PAttachments = attachmentsPtr,
                    SubpassCount = 1,
                    PSubpasses = &subpass,
                    DependencyCount = 1,
                    PDependencies = &dependency,
                };

                if (vk!.CreateRenderPass(_device, renderPassInfo, null, out var renderPass) != Result.Success)
                {
                    throw new Exception("failed to create render pass!");
                }
                return renderPass;
            }
        }

        private RenderPass CreateUiRenderPass()
        {
            AttachmentDescription colorAttachment = new()
            {
                Format = swapChainImageFormat,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Load,
                StoreOp = AttachmentStoreOp.DontCare,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                InitialLayout = ImageLayout.ColorAttachmentOptimal,
                FinalLayout = ImageLayout.ColorAttachmentOptimal,
            };

            AttachmentReference colorAttachmentRef = new()
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal,
            };

            SubpassDescription subpass = new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef,
            };

            SubpassDependency dependency = new()
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
            };

            var attachments = new[] { colorAttachment };

            fixed (AttachmentDescription* attachmentsPtr = attachments)
            {
                RenderPassCreateInfo renderPassInfo = new()
                {
                    SType = StructureType.RenderPassCreateInfo,
                    AttachmentCount = (uint)attachments.Length,
                    PAttachments = attachmentsPtr,
                    SubpassCount = 1,
                    PSubpasses = &subpass,
                    DependencyCount = 1,
                    PDependencies = &dependency,
                };

                if (vk!.CreateRenderPass(_device, renderPassInfo, null, out var renderPass) != Result.Success)
                {
                    throw new Exception("failed to create render pass!");
                }
                return renderPass;
            }
        }

        private void CreateDescriptorSetLayout()
        {
            DescriptorSetLayoutBinding uboLayoutBinding = new()
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                PImmutableSamplers = null,
                StageFlags = ShaderStageFlags.VertexBit,
            };

            DescriptorSetLayoutBinding samplerLayoutBinding = new()
            {
                Binding = 1,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                PImmutableSamplers = null,
                StageFlags = ShaderStageFlags.FragmentBit,
            };

            var bindings = new DescriptorSetLayoutBinding[] { uboLayoutBinding, samplerLayoutBinding };

            fixed (DescriptorSetLayoutBinding* bindingsPtr = bindings)
            fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &_descriptorSetLayout)
            {
                DescriptorSetLayoutCreateInfo layoutInfo = new()
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    BindingCount = (uint)bindings.Length,
                    PBindings = bindingsPtr,
                };

                if (vk!.CreateDescriptorSetLayout(_device, layoutInfo, null, descriptorSetLayoutPtr) != Result.Success)
                {
                    throw new Exception("failed to create descriptor set layout!");
                }
            }
        }

        private void CreateGraphicsPipeline()
        {
            _vulkanPipeline = new VulkanPipeline();

            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();

            fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
            fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &_descriptorSetLayout)
            {

                PipelineVertexInputStateCreateInfo vertexInputInfo = new()
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = 1,
                    VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                    PVertexBindingDescriptions = &bindingDescription,
                    PVertexAttributeDescriptions = attributeDescriptionsPtr,
                };

                PipelineInputAssemblyStateCreateInfo inputAssembly = new()
                {
                    SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                    Topology = PrimitiveTopology.TriangleList,
                    PrimitiveRestartEnable = false,
                };

                Viewport viewport = new()
                {
                    X = 0,
                    Y = 0,
                    Width = swapChainExtent.Width,
                    Height = swapChainExtent.Height,
                    MinDepth = 0,
                    MaxDepth = 1,
                };

                Rect2D scissor = new()
                {
                    Offset = { X = 0, Y = 0 },
                    Extent = swapChainExtent,
                };

                 _graphicsWorldPipelineData = _vulkanPipeline.CreateGraphicsWorldPipeline(vk!, _device, _renderPass, _descriptorSetLayout, viewport, scissor);
                _uiPipelineData = _vulkanPipeline.CreateGraphicsWorldPipeline(vk!, _device, _renderPass, _descriptorSetLayout, viewport, scissor);
            }
        }

        private void CreateFramebuffers()
        {
            _swapChainFramebuffers = new Framebuffer[swapChainImageViews!.Length];
            _uiFramebuffers = new Framebuffer[swapChainImageViews!.Length];

            for (int i = 0; i < swapChainImageViews.Length; i++)
            {
                var attachments = new[] { swapChainImageViews[i], depthImageView };

                fixed (ImageView* attachmentsPtr = attachments)
                {
                    FramebufferCreateInfo framebufferInfo = new()
                    {
                        SType = StructureType.FramebufferCreateInfo,
                        RenderPass = _renderPass,
                        AttachmentCount = (uint)attachments.Length,
                        PAttachments = attachmentsPtr,
                        Width = swapChainExtent.Width,
                        Height = swapChainExtent.Height,
                        Layers = 1,
                    };

                    if (vk!.CreateFramebuffer(_device, framebufferInfo, null, out _swapChainFramebuffers[i]) != Result.Success)
                    {
                        throw new Exception("failed to create framebuffer!");
                    }
                }

                var attachmentsUi = new[] { swapChainImageViews[i] };

                fixed (ImageView* attachmentsPtr = attachmentsUi)
                {

                    FramebufferCreateInfo framebufferInfoUi = new()
                    {
                        SType = StructureType.FramebufferCreateInfo,
                        RenderPass = _uiRenderPass,
                        AttachmentCount = (uint)attachmentsUi.Length,
                        PAttachments = attachmentsPtr,
                        Width = swapChainExtent.Width,
                        Height = swapChainExtent.Height,
                        Layers = 1,
                    };

                    if (vk!.CreateFramebuffer(_device, framebufferInfoUi, null, out _uiFramebuffers[i]) != Result.Success)
                    {
                        throw new Exception("failed to create framebuffer!");
                    }
                }         
            }
        }

        private void CreateCommandPool()
        {
            var queueFamiliyIndicies = FindQueueFamilies(_physicalDevice);

            CommandPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = queueFamiliyIndicies.GraphicsFamily!.Value,
            };

            if (vk!.CreateCommandPool(_device, poolInfo, null, out commandPool) != Result.Success)
            {
                throw new Exception("failed to create command pool!");
            }
        }

        private void CreateDepthResources()
        {
            Format depthFormat = FindDepthFormat();

            CreateImage(swapChainExtent.Width, swapChainExtent.Height, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, ref depthImage, ref depthImageMemory);
            depthImageView = CreateImageView(depthImage, depthFormat, ImageAspectFlags.DepthBit);
        }

        private Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
        {
            foreach (var format in candidates)
            {
                vk!.GetPhysicalDeviceFormatProperties(_physicalDevice, format, out var props);

                if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
                {
                    return format;
                }
                else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
                {
                    return format;
                }
            }

            throw new Exception("failed to find supported format!");
        }

        private Format FindDepthFormat()
        {
            return FindSupportedFormat(new[] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint }, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
        }

        private void CreateTextureImage()
        {
            using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>("Assets/Textures/texture.jpg");

            ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);

            Buffer stagingBuffer = default;
            DeviceMemory stagingBufferMemory = default;
            CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

            void* data;
            vk!.MapMemory(_device, stagingBufferMemory, 0, imageSize, 0, &data);
            img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
            vk!.UnmapMemory(_device, stagingBufferMemory);

            CreateImage((uint)img.Width, (uint)img.Height, Format.R8G8B8A8Srgb, ImageTiling.Optimal, ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit, MemoryPropertyFlags.DeviceLocalBit, ref _textureImage, ref _textureImageMemory);

            TransitionImageLayout(_textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
            CopyBufferToImage(stagingBuffer, _textureImage, (uint)img.Width, (uint)img.Height);
            TransitionImageLayout(_textureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

            vk!.DestroyBuffer(_device, stagingBuffer, null);
            vk!.FreeMemory(_device, stagingBufferMemory, null);
        }

        private void CreateTextureImageView()
        {
            _textureImageView = CreateImageView(_textureImage, Format.R8G8B8A8Srgb, ImageAspectFlags.ColorBit);
        }

        private void CreateTextureSampler()
        {
            vk!.GetPhysicalDeviceProperties(_physicalDevice, out PhysicalDeviceProperties properties);

            SamplerCreateInfo samplerInfo = new()
            {
                SType = StructureType.SamplerCreateInfo,
                MagFilter = Filter.Linear,
                MinFilter = Filter.Linear,
                AddressModeU = SamplerAddressMode.Repeat,
                AddressModeV = SamplerAddressMode.Repeat,
                AddressModeW = SamplerAddressMode.Repeat,
                AnisotropyEnable = true,
                MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
                BorderColor = BorderColor.IntOpaqueBlack,
                UnnormalizedCoordinates = false,
                CompareEnable = false,
                CompareOp = CompareOp.Always,
                MipmapMode = SamplerMipmapMode.Linear,
            };

            fixed (Sampler* textureSamplerPtr = &_textureSampler)
            {
                if (vk!.CreateSampler(_device, samplerInfo, null, textureSamplerPtr) != Result.Success)
                {
                    throw new Exception("failed to create texture sampler!");
                }
            }
        }

        private ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = image,
                ViewType = ImageViewType.Type2D,
                Format = format,
                //Components =
                //    {
                //        R = ComponentSwizzle.Identity,
                //        G = ComponentSwizzle.Identity,
                //        B = ComponentSwizzle.Identity,
                //        A = ComponentSwizzle.Identity,
                //    },
                SubresourceRange =
                {
                    AspectMask = aspectFlags,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }
            };

            if (vk!.CreateImageView(_device, createInfo, null, out ImageView imageView) != Result.Success)
            {
                throw new Exception("failed to create image views!");
            }

            return imageView;
        }

        private void CreateImage(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ref Image image, ref DeviceMemory imageMemory)
        {
            ImageCreateInfo imageInfo = new()
            {
                SType = StructureType.ImageCreateInfo,
                ImageType = ImageType.Type2D,
                Extent =
            {
                Width = width,
                Height = height,
                Depth = 1,
            },
                MipLevels = 1,
                ArrayLayers = 1,
                Format = format,
                Tiling = tiling,
                InitialLayout = ImageLayout.Undefined,
                Usage = usage,
                Samples = SampleCountFlags.Count1Bit,
                SharingMode = SharingMode.Exclusive,
            };

            fixed (Image* imagePtr = &image)
            {
                if (vk!.CreateImage(_device, imageInfo, null, imagePtr) != Result.Success)
                {
                    throw new Exception("failed to create image!");
                }
            }

            vk!.GetImageMemoryRequirements(_device, image, out MemoryRequirements memRequirements);

            MemoryAllocateInfo allocInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
            };

            fixed (DeviceMemory* imageMemoryPtr = &imageMemory)
            {
                if (vk!.AllocateMemory(_device, allocInfo, null, imageMemoryPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate image memory!");
                }
            }

            vk!.BindImageMemory(_device, image, imageMemory, 0);
        }

        private void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands();

            ImageMemoryBarrier barrier = new()
            {
                SType = StructureType.ImageMemoryBarrier,
                OldLayout = oldLayout,
                NewLayout = newLayout,
                SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                Image = image,
                SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            }
            };

            PipelineStageFlags sourceStage;
            PipelineStageFlags destinationStage;

            if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
            {
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;

                sourceStage = PipelineStageFlags.TopOfPipeBit;
                destinationStage = PipelineStageFlags.TransferBit;
            }
            else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
            {
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;

                sourceStage = PipelineStageFlags.TransferBit;
                destinationStage = PipelineStageFlags.FragmentShaderBit;
            }
            else
            {
                throw new Exception("unsupported layout transition!");
            }

            vk!.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, barrier);

            EndSingleTimeCommands(commandBuffer);

        }

        private void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands();

            BufferImageCopy region = new()
            {
                BufferOffset = 0,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
                ImageOffset = new Offset3D(0, 0, 0),
                ImageExtent = new Extent3D(width, height, 1),

            };

            vk!.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, region);

            EndSingleTimeCommands(commandBuffer);
        }

        private void CreateVertexBuffer()
        {
            ulong bufferSize = (ulong)(Unsafe.SizeOf<Vertex>() * vertices.Length);

            Buffer stagingBuffer = default;
            DeviceMemory stagingBufferMemory = default;
            CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

            void* data;
            vk!.MapMemory(_device, stagingBufferMemory, 0, bufferSize, 0, &data);
            vertices.AsSpan().CopyTo(new Span<Vertex>(data, vertices.Length));
            vk!.UnmapMemory(_device, stagingBufferMemory);

            CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref vertexBuffer, ref vertexBufferMemory);

            CopyBuffer(stagingBuffer, vertexBuffer, bufferSize);

            vk!.DestroyBuffer(_device, stagingBuffer, null);
            vk!.FreeMemory(_device, stagingBufferMemory, null);
        }

        private void CreateIndexBuffer()
        {
            ulong bufferSize = (ulong)(Unsafe.SizeOf<ushort>() * indices.Length);

            Buffer stagingBuffer = default;
            DeviceMemory stagingBufferMemory = default;
            CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

            void* data;
            vk!.MapMemory(_device, stagingBufferMemory, 0, bufferSize, 0, &data);
            indices.AsSpan().CopyTo(new Span<ushort>(data, indices.Length));
            vk!.UnmapMemory(_device, stagingBufferMemory);

            CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref indexBuffer, ref indexBufferMemory);

            CopyBuffer(stagingBuffer, indexBuffer, bufferSize);

            vk!.DestroyBuffer(_device, stagingBuffer, null);
            vk!.FreeMemory(_device, stagingBufferMemory, null);
        }

        private void CreateUniformBuffers()
        {
            ulong bufferSize = (ulong)Unsafe.SizeOf<UniformBufferObject>();

            uniformBuffers = new Buffer[swapChainImages!.Length];
            uniformBuffersMemory = new DeviceMemory[swapChainImages!.Length];

            for (int i = 0; i < swapChainImages.Length; i++)
            {
                CreateBuffer(bufferSize, BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref uniformBuffers[i], ref uniformBuffersMemory[i]);
            }

        }

        private void CreateDescriptorPool()
        {
            var poolSizes = new DescriptorPoolSize[]
            {
            new DescriptorPoolSize()
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = (uint)swapChainImages!.Length,
            },
            new DescriptorPoolSize()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = (uint)swapChainImages!.Length,
            }
            };

            fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)
            fixed (DescriptorPool* descriptorPoolPtr = &descriptorPool)
            {

                DescriptorPoolCreateInfo poolInfo = new()
                {
                    SType = StructureType.DescriptorPoolCreateInfo,
                    PoolSizeCount = (uint)poolSizes.Length,
                    PPoolSizes = poolSizesPtr,
                    MaxSets = (uint)swapChainImages!.Length,
                };

                if (vk!.CreateDescriptorPool(_device, poolInfo, null, descriptorPoolPtr) != Result.Success)
                {
                    throw new Exception("failed to create descriptor pool!");
                }

            }
        }

        private void CreateDescriptorSets()
        {
            var layouts = new DescriptorSetLayout[swapChainImages!.Length];
            Array.Fill(layouts, _descriptorSetLayout);

            fixed (DescriptorSetLayout* layoutsPtr = layouts)
            {
                DescriptorSetAllocateInfo allocateInfo = new()
                {
                    SType = StructureType.DescriptorSetAllocateInfo,
                    DescriptorPool = descriptorPool,
                    DescriptorSetCount = (uint)swapChainImages!.Length,
                    PSetLayouts = layoutsPtr,
                };

                descriptorSets = new DescriptorSet[swapChainImages.Length];
                fixed (DescriptorSet* descriptorSetsPtr = descriptorSets)
                {
                    if (vk!.AllocateDescriptorSets(_device, allocateInfo, descriptorSetsPtr) != Result.Success)
                    {
                        throw new Exception("failed to allocate descriptor sets!");
                    }
                }
            }


            for (int i = 0; i < swapChainImages.Length; i++)
            {
                DescriptorBufferInfo bufferInfo = new()
                {
                    Buffer = uniformBuffers![i],
                    Offset = 0,
                    Range = (ulong)Unsafe.SizeOf<UniformBufferObject>(),

                };

                DescriptorImageInfo imageInfo = new()
                {
                    ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                    ImageView = _textureImageView,
                    Sampler = _textureSampler,
                };

                var descriptorWrites = new WriteDescriptorSet[]
                {
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[i],
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                },
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSets[i],
                    DstBinding = 1,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    PImageInfo = &imageInfo,
                }
                };

                fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites)
                {
                    vk!.UpdateDescriptorSets(_device, (uint)descriptorWrites.Length, descriptorWritesPtr, 0, null);
                }
            }

        }

        private void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, ref Buffer buffer, ref DeviceMemory bufferMemory)
        {
            BufferCreateInfo bufferInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = size,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
            };

            fixed (Buffer* bufferPtr = &buffer)
            {
                if (vk!.CreateBuffer(_device, bufferInfo, null, bufferPtr) != Result.Success)
                {
                    throw new Exception("failed to create vertex buffer!");
                }
            }

            MemoryRequirements memRequirements = new();
            vk!.GetBufferMemoryRequirements(_device, buffer, out memRequirements);

            MemoryAllocateInfo allocateInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
            };

            fixed (DeviceMemory* bufferMemoryPtr = &bufferMemory)
            {
                if (vk!.AllocateMemory(_device, allocateInfo, null, bufferMemoryPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate vertex buffer memory!");
                }
            }

            vk!.BindBufferMemory(_device, buffer, bufferMemory, 0);
        }

        private CommandBuffer BeginSingleTimeCommands()
        {
            CommandBufferAllocateInfo allocateInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                Level = CommandBufferLevel.Primary,
                CommandPool = commandPool,
                CommandBufferCount = 1,
            };

            vk!.AllocateCommandBuffers(_device, allocateInfo, out CommandBuffer commandBuffer);

            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
            };

            vk!.BeginCommandBuffer(commandBuffer, beginInfo);

            return commandBuffer;
        }

        private void EndSingleTimeCommands(CommandBuffer commandBuffer)
        {
            vk!.EndCommandBuffer(commandBuffer);

            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &commandBuffer,
            };

            vk!.QueueSubmit(graphicsQueue, 1, submitInfo, default);
            vk!.QueueWaitIdle(graphicsQueue);

            vk!.FreeCommandBuffers(_device, commandPool, 1, commandBuffer);
        }

        private void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
        {
            CommandBuffer commandBuffer = BeginSingleTimeCommands();

            BufferCopy copyRegion = new()
            {
                Size = size,
            };

            vk!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, copyRegion);

            EndSingleTimeCommands(commandBuffer);
        }

        private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
        {
            vk!.GetPhysicalDeviceMemoryProperties(_physicalDevice, out PhysicalDeviceMemoryProperties memProperties);

            for (int i = 0; i < memProperties.MemoryTypeCount; i++)
            {
                if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                {
                    return (uint)i;
                }
            }

            throw new Exception("failed to find suitable memory type!");
        }

        private void CreateCommandBuffers()
        {
            _commandBuffers = new CommandBuffer[_swapChainFramebuffers!.Length];

            CommandBufferAllocateInfo allocInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = commandPool,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = (uint)_commandBuffers.Length,
            };

            fixed (CommandBuffer* commandBuffersPtr = _commandBuffers)
            {
                if (vk!.AllocateCommandBuffers(_device, allocInfo, commandBuffersPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate command buffers!");
                }
            }

            for (int i = 0; i < _commandBuffers.Length; i++)
            {
                CommandBufferBeginInfo beginInfo = new()
                {
                    SType = StructureType.CommandBufferBeginInfo,
                };

                if (vk!.BeginCommandBuffer(_commandBuffers[i], beginInfo) != Result.Success)
                {
                    throw new Exception("failed to begin recording command buffer!");
                }

                DoRenderPassWorld(i, _renderPass);
                //DoRenderPassUi(i, _uiRenderPass);

                if (vk!.EndCommandBuffer(_commandBuffers[i]) != Result.Success)
                {
                    throw new Exception("failed to record command buffer!");
                }
            }
        }

        void DoRenderPassWorld(int bufferIndex, RenderPass renderPass)
        {
            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = renderPass,
                Framebuffer = _swapChainFramebuffers![bufferIndex],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 },
                    Extent = swapChainExtent,
                }
            };
            var color = System.Drawing.Color.CornflowerBlue;
            var clearValues = new ClearValue[]
            {
                new()
                {
                    Color = new (){ Float32_0 = color.R/255f, Float32_1 = color.G/255f, Float32_2 = color.B/255f, Float32_3 = color.A/255f },
                },
                new()
                {
                    DepthStencil = new () { Depth = 1, Stencil = 0 }
                }
            };

            fixed (ClearValue* clearValuesPtr = clearValues)
            {
                renderPassInfo.ClearValueCount = (uint)clearValues.Length;
                renderPassInfo.PClearValues = clearValuesPtr;

                vk!.CmdBeginRenderPass(_commandBuffers![bufferIndex], &renderPassInfo, SubpassContents.Inline);
            }

            vk!.CmdBindPipeline(_commandBuffers[bufferIndex], PipelineBindPoint.Graphics, _graphicsWorldPipelineData.PipelineHandle);

            var vertexBuffers = new Buffer[] { vertexBuffer };
            var offsets = new ulong[] { 0 };

            fixed (ulong* offsetsPtr = offsets)
            fixed (Buffer* vertexBuffersPtr = vertexBuffers)
            {
                vk!.CmdBindVertexBuffers(_commandBuffers[bufferIndex], 0, 1, vertexBuffersPtr, offsetsPtr);
            }

            vk!.CmdBindIndexBuffer(_commandBuffers[bufferIndex], indexBuffer, 0, IndexType.Uint16);

            vk!.CmdBindDescriptorSets(_commandBuffers[bufferIndex], PipelineBindPoint.Graphics, _graphicsWorldPipelineData.PipelineLayout, 0, 1, descriptorSets![bufferIndex], 0, null);

            vk!.CmdDrawIndexed(_commandBuffers[bufferIndex], (uint)indices.Length, 1, 0, 0, 0);

            vk!.CmdEndRenderPass(_commandBuffers[bufferIndex]);
        }

        void DoRenderPassUi(int bufferIndex, RenderPass renderPass)
        {
            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = renderPass,
                Framebuffer = _uiFramebuffers![bufferIndex],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 },
                    Extent = swapChainExtent,
                }
            };

            vk!.CmdBeginRenderPass(_commandBuffers![bufferIndex], &renderPassInfo, SubpassContents.Inline);

            vk!.CmdBindPipeline(_commandBuffers[bufferIndex], PipelineBindPoint.Graphics, _graphicsWorldPipelineData.PipelineHandle);

            var vertexBuffers = new Buffer[] { vertexBuffer };
            var offsets = new ulong[] { 0 };

            fixed (ulong* offsetsPtr = offsets)
            fixed (Buffer* vertexBuffersPtr = vertexBuffers)
            {
                vk!.CmdBindVertexBuffers(_commandBuffers[bufferIndex], 0, 1, vertexBuffersPtr, offsetsPtr);
            }

            vk!.CmdBindIndexBuffer(_commandBuffers[bufferIndex], indexBuffer, 0, IndexType.Uint16);

            vk!.CmdBindDescriptorSets(_commandBuffers[bufferIndex], PipelineBindPoint.Graphics, _uiPipelineData.PipelineLayout, 0, 1, descriptorSets![bufferIndex], 0, null);

            vk!.CmdDrawIndexed(_commandBuffers[bufferIndex], (uint)indices.Length, 1, 0, 0, 0);

            vk!.CmdEndRenderPass(_commandBuffers[bufferIndex]);
        }


        private void CreateSyncObjects()
        {
            imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
            renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
            inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
            imagesInFlight = new Fence[swapChainImages!.Length];

            SemaphoreCreateInfo semaphoreInfo = new()
            {
                SType = StructureType.SemaphoreCreateInfo,
            };

            FenceCreateInfo fenceInfo = new()
            {
                SType = StructureType.FenceCreateInfo,
                Flags = FenceCreateFlags.SignaledBit,
            };

            for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                if (vk!.CreateSemaphore(_device, semaphoreInfo, null, out imageAvailableSemaphores[i]) != Result.Success ||
                    vk!.CreateSemaphore(_device, semaphoreInfo, null, out renderFinishedSemaphores[i]) != Result.Success ||
                    vk!.CreateFence(_device, fenceInfo, null, out inFlightFences[i]) != Result.Success)
                {
                    throw new Exception("failed to create synchronization objects for a frame!");
                }
            }
        }

        private void UpdateUniformBuffer(uint currentImage, double delta)
        {
            //Silk Window has timing information so we are skipping the time code.
            var time = (float)window!.Time;

            //Use elapsed time to convert to radians to allow our cube to rotate over time
            var model = Matrix4X4<float>.Identity * Matrix4X4.CreateFromAxisAngle<float>(new Vector3D<float>(0, 1, 0), time * Scalar.DegreesToRadians(90.0f));
            var view = Matrix4X4.CreateLookAt(CameraPosition, CameraPosition + CameraFront, CameraUp);
            var projection = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(CameraZoom), (float)swapChainExtent.Width / swapChainExtent.Height, 0.1f, 1000.0f);

            //https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%202.2%20-%20Camera/Program.cs
            UniformBufferObject ubo = new()
            {
                model = model,
                view = view,
                proj = projection,
            };
            ubo.proj.M22 *= -1;

            void* data;
            vk!.MapMemory(_device, uniformBuffersMemory![currentImage], 0, (ulong)Unsafe.SizeOf<UniformBufferObject>(), 0, &data);
            new Span<UniformBufferObject>(data, 1)[0] = ubo;
            vk!.UnmapMemory(_device, uniformBuffersMemory![currentImage]);

        }

        private void DrawFrame(double delta)
        {
            var fps = (int)(1000.0 / delta);
            window!.Title = $"{DEFAULT_WINDOW_TITLE} ({fps}) - ({delta})";
            vk!.WaitForFences(_device, 1, inFlightFences![currentFrame], true, ulong.MaxValue);

            uint imageIndex = 0;
            var result = khrSwapChain!.AcquireNextImage(_device, swapChain, ulong.MaxValue, imageAvailableSemaphores![currentFrame], default, ref imageIndex);

            if (result == Result.ErrorOutOfDateKhr)
            {
                RecreateSwapChain();
                return;
            }
            else if (result != Result.Success && result != Result.SuboptimalKhr)
            {
                throw new Exception("failed to acquire swap chain image!");
            }

            UpdateUniformBuffer(imageIndex, delta);

            if (imagesInFlight![imageIndex].Handle != default)
            {
                vk!.WaitForFences(_device, 1, imagesInFlight[imageIndex], true, ulong.MaxValue);
            }
            imagesInFlight[imageIndex] = inFlightFences[currentFrame];

            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
            };

            var waitSemaphores = stackalloc[] { imageAvailableSemaphores[currentFrame] };
            var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

            var buffer = _commandBuffers![imageIndex];

            submitInfo = submitInfo with
            {
                WaitSemaphoreCount = 1,
                PWaitSemaphores = waitSemaphores,
                PWaitDstStageMask = waitStages,

                CommandBufferCount = 1,
                PCommandBuffers = &buffer
            };

            var signalSemaphores = stackalloc[] { renderFinishedSemaphores![currentFrame] };
            submitInfo = submitInfo with
            {
                SignalSemaphoreCount = 1,
                PSignalSemaphores = signalSemaphores,
            };

            vk!.ResetFences(_device, 1, inFlightFences[currentFrame]);

            if (vk!.QueueSubmit(graphicsQueue, 1, submitInfo, inFlightFences[currentFrame]) != Result.Success)
            {
                throw new Exception("failed to submit draw command buffer!");
            }

            var swapChains = stackalloc[] { swapChain };
            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,

                WaitSemaphoreCount = 1,
                PWaitSemaphores = signalSemaphores,

                SwapchainCount = 1,
                PSwapchains = swapChains,

                PImageIndices = &imageIndex
            };

            result = khrSwapChain.QueuePresent(presentQueue, presentInfo);

            if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || frameBufferResized)
            {
                frameBufferResized = false;
                RecreateSwapChain();
            }
            else if (result != Result.Success)
            {
                throw new Exception("failed to present swap chain image!");
            }

            currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;

        }

        private SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
        {
            foreach (var availableFormat in availableFormats)
            {
                if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
                {
                    return availableFormat;
                }
            }

            return availableFormats[0];
        }

        private PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
        {
            foreach (var availablePresentMode in availablePresentModes)
            {
                if (availablePresentMode == PresentModeKHR.MailboxKhr)
                {
                    return availablePresentMode;
                }
            }

            return PresentModeKHR.FifoKhr;
        }

        private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return capabilities.CurrentExtent;
            }
            else
            {
                var framebufferSize = window!.FramebufferSize;

                Extent2D actualExtent = new()
                {
                    Width = (uint)framebufferSize.X,
                    Height = (uint)framebufferSize.Y
                };

                actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
                actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

                return actualExtent;
            }
        }

        private SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice physicalDevice)
        {
            var details = new SwapChainSupportDetails();

            khrSurface!.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out details.Capabilities);

            uint formatCount = 0;
            khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, null);

            if (formatCount != 0)
            {
                details.Formats = new SurfaceFormatKHR[formatCount];
                fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
                {
                    khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, formatsPtr);
                }
            }
            else
            {
                details.Formats = Array.Empty<SurfaceFormatKHR>();
            }

            uint presentModeCount = 0;
            khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, null);

            if (presentModeCount != 0)
            {
                details.PresentModes = new PresentModeKHR[presentModeCount];
                fixed (PresentModeKHR* formatsPtr = details.PresentModes)
                {
                    khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, formatsPtr);
                }

            }
            else
            {
                details.PresentModes = Array.Empty<PresentModeKHR>();
            }

            return details;
        }

        private bool IsDeviceSuitable(PhysicalDevice device)
        {
            var indices = FindQueueFamilies(device);

            bool extensionsSupported = CheckDeviceExtensionsSupport(device);

            bool swapChainAdequate = false;
            if (extensionsSupported)
            {
                var swapChainSupport = QuerySwapChainSupport(device);
                swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
            }

            vk!.GetPhysicalDeviceFeatures(device, out PhysicalDeviceFeatures supportedFeatures);

            return indices.IsComplete() && extensionsSupported && swapChainAdequate && supportedFeatures.SamplerAnisotropy;
        }

        private bool CheckDeviceExtensionsSupport(PhysicalDevice device)
        {
            uint extentionsCount = 0;
            vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, null);

            var availableExtensions = new ExtensionProperties[extentionsCount];
            fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
            {
                vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, availableExtensionsPtr);
            }

            var availableExtensionNames = availableExtensions.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();

            return deviceExtensions.All(availableExtensionNames.Contains);

        }

        private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
        {
            var indices = new QueueFamilyIndices();

            uint queueFamilityCount = 0;
            vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

            var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
            fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
            {
                vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
            }


            uint i = 0;
            foreach (var queueFamily in queueFamilies)
            {
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                {
                    indices.GraphicsFamily = i;
                }

                khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, surface, out var presentSupport);

                if (presentSupport)
                {
                    indices.PresentFamily = i;
                }

                if (indices.IsComplete())
                {
                    break;
                }

                i++;
            }

            return indices;
        }

        private string[] GetRequiredExtensions()
        {
            var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
            var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

            if (EnableValidationLayers)
            {
                return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
            }

            return extensions;
        }

        private bool CheckValidationLayerSupport()
        {
            uint layerCount = 0;
            vk!.EnumerateInstanceLayerProperties(ref layerCount, null);
            var availableLayers = new LayerProperties[layerCount];
            fixed (LayerProperties* availableLayersPtr = availableLayers)
            {
                vk!.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
            }

            var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

            return validationLayers.All(availableLayerNames.Contains);
        }

        private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
        {
            System.Diagnostics.Debug.WriteLine($"validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

            return Vk.False;
        }
    }
}
