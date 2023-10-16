using DragonGameEngine.Core;
using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems.Domain;
using DragonGameEngine.Core.Systems;
using GameEngine.Core.Tests.Mocks;
using Silk.NET.Input;
using Silk.NET.Maths;
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
            var mockRenderer = new MockRenderer();
            var gameEntryMock = new Mock<IGameEntry>();
            var config = ApplicationConfigTestProvider.CreateTestConfig();

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemState(
                    new TextureSystemConfig(65536), new Texture(0, default, EntityIdService.INVALID_ID)
                ));

            var initCalls = 0;
            mockRenderer.OnInit += () =>
            {
                initCalls++;
            };
            var createTextureCalls = 0;
            mockRenderer.OnCreateTexture += (string name, Vector2D<uint> size, byte channelCount, byte[] pixels, bool hasTransparency) =>
            {
                createTextureCalls++;
                return new InnerTexture(size, channelCount, hasTransparency, new object());
            };

            var frontend = new RendererFrontend(config, windowMock.Object, textureSystem, loggerMock.Object, mockRenderer);

            var gameApp = new GameApplication(config, gameEntryMock.Object, windowMock.Object, loggerMock.Object, frontend, textureSystem);

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
            var rendererMock = new Mock<IRenderer>();
            var gameEntryMock = new Mock<IGameEntry>();
            var config = ApplicationConfigTestProvider.CreateTestConfig();

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemState(
                    new TextureSystemConfig(65536), new Texture(0, default, EntityIdService.INVALID_ID)
                ));

            var frontend = new RendererFrontend(config, windowMock.Object, textureSystem, loggerMock.Object, rendererMock.Object);

            var gameApp = new GameApplication(config, gameEntryMock.Object, windowMock.Object, loggerMock.Object, frontend, textureSystem);

            gameApp.Shutdown();

            rendererMock.Verify((renderer) => renderer.Shutdown(), Times.Once());
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