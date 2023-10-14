using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using GameEngine.Core.Tests.Mocks;
using Silk.NET.Maths;
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
            var mockRenderer = new MockRenderer();
            var config = ApplicationConfigTestProvider.CreateTestConfig();

            var frontend = new RendererFrontend(config, windowMock.Object, loggerMock.Object, mockRenderer);

            var initCalls = 0;
            mockRenderer.OnInit += (texture) =>
            {
                initCalls++;
            };
            var createTextureCalls = 0;
            mockRenderer.OnCreateTexture += (string name, bool autoRelease, Vector2D<uint> size, byte channelCount, byte[] pixels, bool hasTransparency) =>
            {
                createTextureCalls++;
                return new InnerTexture(size, channelCount, hasTransparency, new object());
            };

            frontend.Init();

            Assert.AreEqual(1, initCalls, "Expected init to be called once.");
            Assert.IsTrue(createTextureCalls >= 1, "Expected create texture to be called at least once.");
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