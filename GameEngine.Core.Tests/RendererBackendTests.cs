using DragonGameEngine.Core.Rendering;
using Silk.NET.Windowing;

namespace GameEngine.Core.Tests
{
    [TestClass]
    public class RendererBackendTests
    {
        [TestMethod]
        public void RendererBackend_Init_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var windowMock = new Mock<IWindow>();
            var rendererMock = new Mock<IRenderer>();

            var backend = new RendererBackend(windowMock.Object, loggerMock.Object, rendererMock.Object);

            backend.Init();

            rendererMock.Verify((renderer) => renderer.Init(), Times.Once());
        }

        [TestMethod]
        public void RendererBackend_Shutdown_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var windowMock = new Mock<IWindow>();
            var rendererMock = new Mock<IRenderer>();

            var backend = new RendererBackend(windowMock.Object, loggerMock.Object, rendererMock.Object);

            backend.Shutdown();

            rendererMock.Verify((renderer) => renderer.Shutdown(), Times.Once());
        }
    }
}