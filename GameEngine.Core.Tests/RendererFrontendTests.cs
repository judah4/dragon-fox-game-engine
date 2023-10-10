using DragonGameEngine.Core.Rendering;
using Silk.NET.Windowing;

namespace GameEngine.Core.Tests
{
    [TestClass]
    public class RendererFrontendTests
    {
        [TestMethod]
        public void RendererFrontend_Init_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var windowMock = new Mock<IWindow>();
            var rendererMock = new Mock<IRenderer>();
            var config = ApplicationConfigTestProvider.CreateTestConfig();

            var frontend = new RendererFrontend(config, windowMock.Object, loggerMock.Object, rendererMock.Object);

            frontend.Init();

            rendererMock.Verify((renderer) => renderer.Init(), Times.Once());
        }

        [TestMethod]
        public void RendererFrontend_Shutdown_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var windowMock = new Mock<IWindow>();
            var rendererMock = new Mock<IRenderer>();
            var config = ApplicationConfigTestProvider.CreateTestConfig();

            var frontend = new RendererFrontend(config, windowMock.Object, loggerMock.Object, rendererMock.Object);

            frontend.Shutdown();

            rendererMock.Verify((renderer) => renderer.Shutdown(), Times.Once());
        }
    }
}