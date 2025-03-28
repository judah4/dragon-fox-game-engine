using DragonGameEngine.Core;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems.Domain;
using DragonGameEngine.Core.Systems;
using GameEngine.Core.Tests.Mocks;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace GameEngine.Core.Tests
{
    [TestClass]
    public class GameApplicationTests
    {
        [TestMethod]
        public void GameApplication_Init_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var windowMock = MockWindow();
            var mockRenderer = new MockRendererFrontend();
            var gameEntryMock = new Mock<IGameEntry>();
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

            var geometrySystem = new GeometrySystem(
                loggerMock.Object,
                new GeometrySystemConfig(4096),
                materialSystem,
                resourceSystem);

            var initCalls = 0;
            mockRenderer.OnInit += () =>
            {
                initCalls++;
            };
            var createTextureCalls = 0;
            mockRenderer.OnLoadTexture += (byte[] pixels, Texture texture) =>
            {
                createTextureCalls++;
                texture.UpdateTextureInternalData(new object());
            };

            var gameApp = new GameApplication(config, gameEntryMock.Object, windowMock.Object, loggerMock.Object, mockRenderer, textureSystem, materialSystem, geometrySystem, resourceSystem);

            gameApp.Init();

            Assert.AreEqual(1, initCalls, "Expected init to be called once.");
            Assert.IsTrue(createTextureCalls >= 1, "Expected create texture to be called at least once.");
            gameEntryMock.Verify((gameEntry) => gameEntry.Initialize(It.IsAny<GameApplication>()), Times.Once());
        }

        [TestMethod]
        public void GameApplication_Shutdown_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var windowMock = MockWindow();
            var mockRenderer = new MockRendererFrontend();
            var gameEntryMock = new Mock<IGameEntry>();
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

            var geometrySystem = new GeometrySystem(
                loggerMock.Object,
                new GeometrySystemConfig(4096),
                materialSystem,
                resourceSystem);


            var shutdownCalls = 0;
            mockRenderer.OnShutdown += () =>
            {
                shutdownCalls++;
            };

            var gameApp = new GameApplication(config, gameEntryMock.Object, windowMock.Object, loggerMock.Object, mockRenderer, textureSystem, materialSystem, geometrySystem, resourceSystem);

            gameApp.Shutdown();

            Assert.AreEqual(1, shutdownCalls, "Expected shutdown to be called once.");
            gameEntryMock.Verify((gameEntry) => gameEntry.Shutdown(), Times.Once());

        }

        private static Mock<IWindow> MockWindow()
        {
            var inputPlatformMock = new Mock<IInputPlatform>();
            var inputContextMock = new Mock<IInputContext>();
            var keyboardMock = new Mock<IKeyboard>();
            inputContextMock.Setup(inputContext => inputContext.Keyboards).Returns(new[] { keyboardMock.Object });
            inputPlatformMock.Setup(inputPlatform => inputPlatform.IsApplicable(It.IsAny<IView>())).Returns(true);
            inputPlatformMock.Setup(inputPlatform => inputPlatform.CreateInput(It.IsAny<IView>())).Returns(inputContextMock.Object);
            InputWindowExtensions.Add(inputPlatformMock.Object);
            var windowMock = new Mock<IWindow>();
            windowMock.Setup(window => window.IsInitialized).Returns(true);

            return windowMock;
        }
    }
}