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
                    new TextureSystemConfig(65536), new Texture(0, default, EntityIdService.INVALID_ID)
                );
            var textureSystem = new TextureSystem(
                loggerMock.Object,
                textureSystemState
                );

            var createTextureCalls = 0;
            var innerData = 100;
            mockRenderer.OnCreateTexture += (string name, Vector2D<uint> size, byte channelCount, byte[] pixels, bool hasTransparency) =>
            {
                createTextureCalls++;
                return new InnerTexture(size, channelCount, hasTransparency, innerData);
            };

            textureSystem.Init(mockRenderer);

            Assert.AreEqual(1, createTextureCalls, "Expected creae texture to be called once for the default texture.");
            Assert.AreEqual(innerData, textureSystemState.DefaultTexture.Data.InternalData);
        }

        [TestMethod]
        public void TextureSystem_Shutdown_Test()
        {
            var loggerMock = new Mock<ILogger>();

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemState(
                    new TextureSystemConfig(65536), new Texture(0, default, EntityIdService.INVALID_ID)
                ));

            textureSystem.Shutdown();

        }
    }
}