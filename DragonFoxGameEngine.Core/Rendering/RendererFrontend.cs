using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Platforms;
using DragonGameEngine.Core.Rendering.Headless;
using DragonGameEngine.Core.Rendering.Vulkan;
using DragonGameEngine.Core.Resources;
using Foxis.Library;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Drawing;
using System.IO;

namespace DragonGameEngine.Core.Rendering
{
    public sealed class RendererFrontend
    {
        private readonly IWindow _window;
        private readonly ApplicationConfig _config;

        private readonly ILogger _logger;
        private readonly IRenderer _rendererBackend;

        private RenderSystemState _systemState;
        private float rotationAngle = 0f;

        private readonly Texture _defaultTexture;
        private readonly Texture _testDiffuse; //temp texture
        private int _testTextureChoice;

        public RendererFrontend(ApplicationConfig config, IWindow window, ILogger logger, IRenderer renderer)
        {
            _config = config;
            _window = window;
            _logger = logger;
            _rendererBackend = renderer;

            _defaultTexture = new Texture(
                0, //id
                default,
                EntityIdService.INVALID_ID);
            //manually set to invalid generation for default

            //TODO: Load other textures
            _testDiffuse = new Texture(
                1,
                default,
                EntityIdService.INVALID_ID);

        }

        public RendererFrontend(ApplicationConfig config, IWindow window, ILogger logger)
            : this(config, window, logger, SetupRenderer(config, window, logger))
        {
        }

        public void Init()
        {
            _rendererBackend.Init(_defaultTexture);

            var initialView = Matrix4X4.CreateTranslation(new Vector3D<float>(0, 0, 10));
            Matrix4X4.Invert(initialView, out initialView);

            GenerateDefaultTexture();

            _systemState = RegenProjectionMatrix(0.1f, 1000.0f)
                .UpdateView(initialView);
        }

        public void Shutdown()
        {
            DestroyTexture(_testDiffuse);
            DestroyTexture(_defaultTexture);
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
                var geometryRenderData = new GeometryRenderData()
                {
                    Model = model,
                    ObjectId = 0,
                    Textures = new Texture[] { _testDiffuse },
                };
                _rendererBackend.UpdateObject(geometryRenderData);

                _rendererBackend.EndFrame(packet.DeltaTime);
            }
        }

        public void SetView(Matrix4X4<float> view)
        {
            _systemState = _systemState.UpdateView(view);
        }

        public InnerTexture CreateTexture(string name, bool autoRelease, Vector2D<uint> size, byte channelCount, Span<byte> pixels, bool hasTransparency)
        {
            return _rendererBackend.CreateTexture(name, autoRelease, size, channelCount, pixels, hasTransparency);
        }

        public void DestroyTexture(Texture texture)
        {
            _rendererBackend.DestroyTexture(texture);
        }

        private RenderSystemState RegenProjectionMatrix(float nearClip, float farClip)
        {
            var projection = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(45f), (float)_window.Size.X / (float)_window.Size.Y, nearClip, farClip);
            var renderSystemState = new RenderSystemState(projection, _systemState.View, farClip, nearClip);
            return renderSystemState;
        }

        private void GenerateDefaultTexture()
        {
            // NOTE: Create default texture, a 256x256 blue/white checkerboard pattern.
            // This is done in code to eliminate asset dependencies.
            _logger.LogDebug("Creating default texture...");
            const uint tex_dimension = 256;
            const uint channels = 4;
            const uint pixel_count = tex_dimension * tex_dimension;
            byte[] pixels = new byte[pixel_count * channels];
            Array.Fill<byte>(pixels, 255); //set full
            // Each pixel.
            for (uint row = 0; row < tex_dimension; ++row)
            {
                for (uint col = 0; col < tex_dimension; ++col)
                {
                    //this is all hard to follow but it makes a tile.
                    uint index = (row * tex_dimension) + col;
                    uint index_bpp = index * channels;
                    if (row % 2 == 0)
                    {
                        if (col % 2 == 0)
                        {
                            pixels[index_bpp + 0] = 0;
                            pixels[index_bpp + 1] = 0;
                        }
                    }
                    else
                    {
                        if (!(col % 2 == 0))
                        {
                            pixels[index_bpp + 0] = 0;
                            pixels[index_bpp + 1] = 0;
                        }
                    }
                }
            }
            _defaultTexture.UpdateTexture(
                CreateTexture(
                    "default",
                    false,
                    new Vector2D<uint>(tex_dimension, tex_dimension),
                    4,
                    pixels,
                    false),
                EntityIdService.INVALID_ID);
            //manually set to invalid generation for default

        }

        private Result<bool> LoadTexture(string textureName, Texture texture)
        {
            //TODO: should be able to be located anywhere.
            var path = "Assets/Textures/";
            const int requiredChannelCount = 4;

            //todo: try different extensions
            var filePath = Path.Join(path, $"{textureName}.png");

            try
            {
                using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(filePath);

                ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);
                var pixels = new byte[imageSize];
                img.CopyPixelDataTo(pixels);

                uint currentGeneration = texture.Generation;
                texture.ResetGeneration();
                
                // Check for transparency
                bool hasTransparency = img.PixelType.AlphaRepresentation.HasValue && img.PixelType.AlphaRepresentation.Value != PixelAlphaRepresentation.None;

                var innerTexture = CreateTexture(
                    textureName,
                    true,
                    new Vector2D<uint>((uint)img.Width, (uint)img.Height),
                    requiredChannelCount,
                    pixels,
                    hasTransparency);

                DestroyTexture(texture);

                uint newGeneration = 0;
                if(currentGeneration != EntityIdService.INVALID_ID)
                {
                    newGeneration = currentGeneration + 1;
                }

                texture.UpdateTexture(innerTexture, newGeneration);

            }
            catch (Exception e) 
            {
                _logger.LogError(e, e.Message);
                return Result.Fail<bool>(e.Message);
            }
            return Result.Ok<bool>();
        }

        private static IRenderer SetupRenderer(ApplicationConfig config, IWindow window, ILogger logger)
        {
            IRenderer renderer;
            if (!config.HeadlessMode)
            {
                renderer = new VulkanBackendRenderer(config.Title, window, logger);
            }
            else
            {
                renderer = new HeadlessRenderer(logger);
            }

            return new RendererBackend(window, logger, renderer);
        }

        public void CycleTestTexture()
        {
            var textureNames = new string[]
            {
                "cobblestone",
                "paving",
                "paving2",
                "CoffeeDragon",
            };

            _testTextureChoice++;
            _testTextureChoice %= textureNames.Length;

            LoadTexture(textureNames[_testTextureChoice], _testDiffuse);

            throw new NotImplementedException();
        }
    }
}
