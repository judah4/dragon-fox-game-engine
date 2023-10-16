using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using DragonGameEngine.Core.Systems.Domain;
using GameEngine.Core.Tests.Mocks;
using Silk.NET.Windowing;

namespace GameEngine.Core.Tests
{
    [TestClass]
    public class TextureSystemTests
    {
        [TestMethod]
        public void RendererFrontend_Init_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var windowMock = new Mock<IWindow>();
            var mockRenderer = new MockRenderer();
            var config = ApplicationConfigTestProvider.CreateTestConfig();
            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemState(
                    new TextureSystemConfig(65536), new Texture(0, default, EntityIdService.INVALID_ID)
                ));
            var frontend = new RendererFrontend(config, windowMock.Object, textureSystem, loggerMock.Object, mockRenderer);

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

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemState(
                    new TextureSystemConfig(65536), new Texture(0, default, EntityIdService.INVALID_ID)
                ));

            var frontend = new RendererFrontend(config, windowMock.Object, textureSystem, loggerMock.Object, rendererMock.Object);

            frontend.Shutdown();

            rendererMock.Verify((renderer) => renderer.Shutdown(), Times.Once());
        }
    }
}