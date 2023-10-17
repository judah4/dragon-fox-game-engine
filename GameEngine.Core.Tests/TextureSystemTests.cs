using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using DragonGameEngine.Core.Systems.Domain;
using GameEngine.Core.Tests.Mocks;
using Silk.NET.Maths;

namespace GameEngine.Core.Tests
{
    [TestClass]
    public class TextureSystemTests
    {
        [TestMethod]
        public void TextureSystem_Init_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRenderer();
            var textureSystemState = new TextureSystemState(
                    new TextureSystemConfig(65536), new Texture(TextureSystem.DEFAULT_TEXTURE_NAME));
            var textureSystem = new TextureSystem(
                loggerMock.Object,
                textureSystemState);

            var createTextureCalls = 0;
            var innerData = 100;
            mockRenderer.OnLoadTexture += (byte[] pixels, Texture texture) =>
            {
                createTextureCalls++;
                texture.UpdateTextureInternalData(innerData);
            };

            textureSystem.Init(mockRenderer);

            Assert.AreEqual(1, createTextureCalls, "Expected creae texture to be called once for the default texture.");
            Assert.AreEqual(innerData, textureSystemState.DefaultTexture.InternalData);
        }

        [TestMethod]
        public void TextureSystem_Shutdown_Test()
        {
            var loggerMock = new Mock<ILogger>();

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemState(
                    new TextureSystemConfig(65536), new Texture(TextureSystem.DEFAULT_TEXTURE_NAME)
                ));

            textureSystem.Shutdown();
        }

        [TestMethod]
        public void TextureSystem_Acquire_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRenderer();
            var textureSystemState = new TextureSystemState(
                    new TextureSystemConfig(65536), new Texture(TextureSystem.DEFAULT_TEXTURE_NAME)
                );
            var textureSystem = new TextureSystem(
                loggerMock.Object,
                textureSystemState);

            mockRenderer.OnLoadTexture += (byte[] pixels, Texture texture) =>
            {
                texture.UpdateTextureInternalData(new object());
            };

            textureSystem.Init(mockRenderer);

            var texture = textureSystem.Acquire("TestTexture", true);

            Assert.IsNotNull(texture);
            Assert.AreEqual(1, textureSystemState.Textures.Count, "Expected a texture to be saved.");
            Assert.AreNotEqual(0U, texture.Id, "Id should be generated properly.");
            Assert.AreEqual(0U, texture.Generation, "Generation should be set.");

        }

        [TestMethod]
        public void TextureSystem_Release_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRenderer();
            var textureSystemState = new TextureSystemState(
                    new TextureSystemConfig(65536), new Texture(TextureSystem.DEFAULT_TEXTURE_NAME));

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                textureSystemState);

            mockRenderer.OnLoadTexture += (byte[] pixels, Texture texture) =>
            {
                texture.UpdateTextureInternalData(new object());
            };

            textureSystem.Init(mockRenderer);

            var textureName = "TestTexture";

            var texture = textureSystem.Acquire("TestTexture", true);

            textureSystem.Release(textureName);

            Assert.AreEqual(0, textureSystemState.Textures.Count);
            Assert.AreEqual(EntityIdService.INVALID_ID, texture.Generation);

        }
    }
}