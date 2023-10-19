using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using DragonGameEngine.Core.Systems.Domain;
using GameEngine.Core.Tests.Mocks;
using Silk.NET.Maths;

namespace GameEngine.Core.Tests.Systems
{
    [TestClass]
    public class MaterialSystemTests
    {
        [TestMethod]
        public void MaterialSystem_Init_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRenderer();

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemConfig(65536));

            var materialSystem = new MaterialSystem(
                loggerMock.Object,
                new MaterialSystemConfig(1024),
                textureSystem);

            var loadMaterialCalls = 0;
            uint expectedInternalId = 0;
            mockRenderer.OnLoadMaterial += (Material material) =>
            {
                loadMaterialCalls++;
                material.UpdateInternalId(expectedInternalId); //set the id for being loaded
            };

            materialSystem.Init(mockRenderer);

            Assert.AreEqual(1, loadMaterialCalls, "Expected creae material to be called once for the default material.");
            Assert.AreEqual(expectedInternalId, materialSystem.Acquire(MaterialSystem.DEFAULT_MATERIAL_NAME).InternalId);
        }

        [TestMethod]
        public void MaterialSystem_Shutdown_Test()
        {
            var loggerMock = new Mock<ILogger>();

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemConfig(65536));

            var materialSystem = new MaterialSystem(
                loggerMock.Object,
                new MaterialSystemConfig(4096),
                textureSystem);

            materialSystem.Shutdown();
        }

        [TestMethod]
        public void MaterialSystem_Acquire_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRenderer();
            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemConfig(65536));

            var materialSystem = new MaterialSystem(
                loggerMock.Object,
                new MaterialSystemConfig(4096),
                textureSystem);

            uint expectedInternalId = 1;
            mockRenderer.OnLoadMaterial += (Material material) =>
            {
                material.UpdateInternalId(expectedInternalId); //set the id for being loaded
            };

            materialSystem.Init(mockRenderer);

            var material = materialSystem.AcquireFromConfig(new MaterialConfig()
            {
                Name = "TestMaterial",
                AutoRelease = true,
                DiffuseColor = Vector4D<float>.One,
                DiffuseMapName = "TestTexture",
            });

            Assert.IsNotNull(material);
            Assert.AreEqual(1, materialSystem.MaterialsCount, "Expected the material to be saved.");
            Assert.AreEqual(expectedInternalId, material.InternalId, "Id should be generated properly.");
            Assert.AreEqual(0U, material.Generation, "Generation should be set.");

        }

        [TestMethod]
        public void MaterialSystem_Release_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRenderer();

            var textureSystem = new TextureSystem(
                loggerMock.Object,
                new TextureSystemConfig(65536));

            var materialSystem = new MaterialSystem(
                loggerMock.Object,
                new MaterialSystemConfig(4096),
                textureSystem);


            uint expectedInternalId = 1;
            mockRenderer.OnLoadMaterial += (Material material) =>
            {
                material.UpdateInternalId(expectedInternalId); //set the id for being loaded
            };

            materialSystem.Init(mockRenderer);

            var materialName = "TestMaterial";

            var material = materialSystem.AcquireFromConfig(new MaterialConfig()
            {
                Name = "TestMaterial",
                AutoRelease = true,
                DiffuseColor = Vector4D<float>.One,
                DiffuseMapName = "TestTexture",
            });

            materialSystem.Release(materialName);

            Assert.AreEqual(0, materialSystem.MaterialsCount);
            Assert.AreEqual(EntityIdService.INVALID_ID, material.Generation);

        }
    }
}