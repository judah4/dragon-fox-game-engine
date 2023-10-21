using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Systems;
using DragonGameEngine.Core.Systems.Domain;
using GameEngine.Core.Tests.Mocks;

namespace GameEngine.Core.Tests.Systems
{
    [TestClass]
    public class TextureSystemTests
    {
        [TestMethod]
        public void TextureSystem_Init_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRendererFrontend();
            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemConfig(65536));

            var createTextureCalls = 0;
            var innerData = 100;
            mockRenderer.OnLoadTexture += (pixels, texture) =>
            {
                createTextureCalls++;
                texture.UpdateTextureInternalData(innerData);
            };

            textureSystem.Init(mockRenderer);

            Assert.AreEqual(1, createTextureCalls, "Expected creae texture to be called once for the default texture.");
            Assert.AreEqual(innerData, textureSystem.GetDefaultTexture().InternalData);
        }

        [TestMethod]
        public void TextureSystem_Shutdown_Test()
        {
            var loggerMock = new Mock<ILogger>();

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemConfig(65536));

            textureSystem.Shutdown();
        }

        [TestMethod]
        public void TextureSystem_Acquire_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRendererFrontend();
            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemConfig(65536));

            mockRenderer.OnLoadTexture += (pixels, texture) =>
            {
                texture.UpdateTextureInternalData(new object());
            };

            textureSystem.Init(mockRenderer);

            var texture = textureSystem.Acquire("TestTexture", true);

            Assert.IsNotNull(texture);
            Assert.AreEqual(1, textureSystem.TexturesCount, "Expected a texture to be saved.");
            Assert.AreNotEqual(0U, texture.Id, "Id should be generated properly.");
            Assert.AreEqual(0U, texture.Generation, "Generation should be set.");

        }

        [TestMethod]
        public void TextureSystem_Release_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRendererFrontend();

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemConfig(65536));

            mockRenderer.OnLoadTexture += (pixels, texture) =>
            {
                texture.UpdateTextureInternalData(new object());
            };

            textureSystem.Init(mockRenderer);

            var textureName = "TestTexture";

            var texture = textureSystem.Acquire("TestTexture", true);

            textureSystem.Release(textureName);

            Assert.AreEqual(0, textureSystem.TexturesCount);
            Assert.AreEqual(EntityIdService.INVALID_ID, texture.Generation);

        }
    }
}