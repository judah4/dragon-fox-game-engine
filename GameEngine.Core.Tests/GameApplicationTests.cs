using DragonGameEngine.Core;
using DragonGameEngine.Core.Rendering;
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
            var rendererMock = new Mock<IRenderer>();
            var gameEntryMock = new Mock<IGameEntry>();
            var config = ApplicationConfigTestProvider.CreateTestConfig();

            var frontend = new RendererFrontend(config, windowMock.Object, loggerMock.Object, rendererMock.Object);

            var gameApp = new GameApplication(config, gameEntryMock.Object, windowMock.Object, loggerMock.Object, frontend);

            gameApp.Init();

            rendererMock.Verify((renderer) => renderer.Init(), Times.Once());
            gameEntryMock.Verify((gameEntry) => gameEntry.Initialize(It.IsAny<IWindow>()), Times.Once());
        }

        [TestMethod]
        public void GameApplication_Shutdown_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var windowMock = MockWindow();
            var rendererMock = new Mock<IRenderer>();
            var gameEntryMock = new Mock<IGameEntry>();
            var config = ApplicationConfigTestProvider.CreateTestConfig();

            var frontend = new RendererFrontend(config, windowMock.Object, loggerMock.Object, rendererMock.Object);

            var gameApp = new GameApplication(config, gameEntryMock.Object, windowMock.Object, loggerMock.Object, frontend);

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