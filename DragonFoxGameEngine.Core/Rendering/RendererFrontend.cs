using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Platforms;
using DragonGameEngine.Core.Rendering.Headless;
using DragonGameEngine.Core.Rendering.Vulkan;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using DragonGameEngine.Core.Systems.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;
using System.Drawing;

namespace DragonGameEngine.Core.Rendering
{
    public sealed class RendererFrontend
    {
        public IRenderer Renderer => _rendererBackend;

        private readonly ApplicationConfig _config;
        private readonly IWindow _window;
        private readonly TextureSystem _textureSystem;
        private readonly MaterialSystem _materialSystem;

        private readonly ILogger _logger;

        private readonly IRenderer _rendererBackend;

        private RenderSystemState _systemState;
        private float rotationAngle = 0f;

        private Material? _testMaterial; //temp material
        private int _testTextureChoice;

        public RendererFrontend(ApplicationConfig config, IWindow window, TextureSystem textureSystem, MaterialSystem materialSystem, ILogger logger, IRenderer renderer)
        {
            _config = config;
            _window = window;
            _textureSystem = textureSystem;
            _materialSystem = materialSystem;
            _logger = logger;
            _rendererBackend = renderer;
        }

        public RendererFrontend(ApplicationConfig config, IWindow window, TextureSystem textureSystem, MaterialSystem materialSystem, ILogger logger)
            : this(config, window, textureSystem, materialSystem, logger, SetupRenderer(config, window, textureSystem, logger))
        {
        }

        public void Init()
        {
            _rendererBackend.Init();

            var initialView = Matrix4X4.CreateTranslation(new Vector3D<float>(0, 0, 10));
            Matrix4X4.Invert(initialView, out initialView);

            _systemState = RegenProjectionMatrix(0.1f, 1000.0f)
                .UpdateView(initialView);
        }

        public void Shutdown()
        {
            _rendererBackend.Shutdown();
        }

        public void Resized(Vector2D<uint> size)
        {
            _systemState = RegenProjectionMatrix(_systemState.NearClipPlane, _systemState.FarClipPlane);
            _rendererBackend.Resized(size);
        }

        public void DrawFrame(RenderPacket packet)
        {
            if (_rendererBackend.BeginFrame(packet.DeltaTime))
            {
                _rendererBackend.UpdateGlobalState(_systemState.Projection, _systemState.View, Vector3D<float>.Zero, Color.White, 0);


                rotationAngle += 1f * (float)packet.DeltaTime;
                var rotation = Quaternion<float>.CreateFromAxisAngle(new Vector3D<float>(0, 0, 1), rotationAngle);
                // model is the object's matrix. Postion, rotation, and scale
                var model = Matrix4X4.CreateFromQuaternion(rotation);

                //TODO: Temporary
                if(_testMaterial == null)
                {
                    try
                    {
                        _testMaterial = _materialSystem.Acquire("test_material");
                    }
                    catch(Exception e)
                    {
                        _logger.LogError(e, e.Message);
                        //back up creation if it failed
                        var matConfig = new MaterialConfig("test_material", false, Vector4D<float>.One, TextureSystem.DEFAULT_TEXTURE_NAME);
                        _testMaterial = _materialSystem.AcquireFromConfig(matConfig);
                    }
                }

                var geometryRenderData = new GeometryRenderData()
                {
                    Model = model,
                    Material = _testMaterial,
                };
                _rendererBackend.UpdateObject(geometryRenderData);

                _rendererBackend.EndFrame(packet.DeltaTime);
            }
        }

        public void SetView(Matrix4X4<float> view)
        {
            _systemState = _systemState.UpdateView(view);
        }

        public void LoadMaterial(Material material)
        {
            _rendererBackend.LoadMaterial(material);
        }

        public void DestroyMaterial(Material material)
        {
            _rendererBackend.DestroyMaterial(material);
        }

        private RenderSystemState RegenProjectionMatrix(float nearClip, float farClip)
        {
            var projection = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45f), (float)_window.Size.X / (float)_window.Size.Y, nearClip, farClip);
            var renderSystemState = new RenderSystemState(projection, _systemState.View, farClip, nearClip);
            return renderSystemState;
        }

        public void CycleTestTexture()
        {
            if (_textureSystem == null)
            {
                _logger.LogError("Renderer not initialized");
                return;
            }
            if(_testMaterial == null)
            {
                _logger.LogError("Renderer not initialized");
                return;
            }

            var textureNames = new string[]
            {
                "cobblestone",
                "paving",
                "paving2",
                "CoffeeDragon",
            };

            var oldName = textureNames[_testTextureChoice];

            _testTextureChoice++;
            _testTextureChoice %= textureNames.Length;
            Texture texture;
            try
            {
                texture = _textureSystem.Acquire(textureNames[_testTextureChoice], true);
            }
            catch(Exception e)
            {
                _logger.LogError(e, $"No texture to load, using default - {e.Message}");
                texture = _textureSystem.GetDefaultTexture();
            } 

            _testMaterial.UpdateMetaData(_testMaterial.DiffuseColor, new TextureMap()
            {
                Texture = texture,
                TextureUse = TextureUse.MapDiffuse,
            });

            _textureSystem.Release(oldName);
        }

        private static IRenderer SetupRenderer(ApplicationConfig config, IWindow window, TextureSystem textureSystem, ILogger logger)
        {
            IRenderer renderer;
            if (!config.HeadlessMode)
            {
                renderer = new VulkanBackendRenderer(config.Title, window, textureSystem, logger);
            }
            else
            {
                renderer = new HeadlessRenderer(logger);
            }

            return renderer;
        }
    }
}
