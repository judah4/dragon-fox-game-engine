using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using DragonGameEngine.Core.Systems.Domain;
using GameEngine.Core.Tests.Mocks;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace GameEngine.Core.Tests.Rendering
{
    [TestClass]
    public class RendererFrontendTests
    {
        [TestMethod]
        public void RendererFrontend_Init_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var windowMock = new Mock<IWindow>();
            var mockRenderer = new MockBackendRenderer();
            var config = ApplicationConfigTestProvider.CreateTestConfig();
            var resourceSystem = new ResourceSystem(
                loggerMock.Object,
                new ResourceSystemConfig(32, "Assets"));

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemConfig(65536),
                resourceSystem);

            var materialSystem = new MaterialSystem(
                loggerMock.Object,
                new MaterialSystemConfig(4096),
                textureSystem,
                resourceSystem);

            var frontend = new RendererFrontend(config, windowMock.Object, textureSystem, materialSystem, loggerMock.Object, mockRenderer);

            var initCalls = 0;
            mockRenderer.OnInit += () =>
            {
                initCalls++;
            };

            frontend.Init();

            Assert.AreEqual(1, initCalls, "Expected init to be called once.");
        }

        [TestMethod]
        public void RendererFrontend_Shutdown_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var windowMock = new Mock<IWindow>();
            var rendererMock = new Mock<IRenderer>();
            var config = ApplicationConfigTestProvider.CreateTestConfig();

            var resourceSystem = new ResourceSystem(
                loggerMock.Object,
                new ResourceSystemConfig(32, "Assets"));

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemConfig(65536),
                resourceSystem);

            var materialSystem = new MaterialSystem(
                loggerMock.Object,
                new MaterialSystemConfig(4096),
                textureSystem,
                resourceSystem);

            var frontend = new RendererFrontend(config, windowMock.Object, textureSystem, materialSystem, loggerMock.Object, rendererMock.Object);

            frontend.Shutdown();

            rendererMock.Verify((renderer) => renderer.Shutdown(), Times.Once());
        }
    }
}