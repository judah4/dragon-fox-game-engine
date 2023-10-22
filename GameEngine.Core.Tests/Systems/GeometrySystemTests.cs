using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems;
using DragonGameEngine.Core.Systems.Domain;
using GameEngine.Core.Tests.Mocks;

namespace GameEngine.Core.Tests.Systems
{
    [TestClass]
    public class GeometrySystemTests
    {
        [TestMethod]
        public void GeometrySystem_Init_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRendererFrontend();

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

            var loadGeometryCalls = 0;
            uint expectedInternalId = 0;
            mockRenderer.OnLoadGeometry += (Geometry geometry) =>
            {
                loadGeometryCalls++;
                geometry.UpdateInternalId(expectedInternalId); //set the id for being loaded
            };

            geometrySystem.Init(mockRenderer);

            Assert.AreEqual(1, loadGeometryCalls, "Expected create geometry to be called once for the default geometry.");
            Assert.AreEqual(expectedInternalId, geometrySystem.GetDefaultGeometry().InternalId);
        }

        [TestMethod]
        public void GeometrySystem_Shutdown_Test()
        {
            var loggerMock = new Mock<ILogger>();

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

            geometrySystem.Shutdown();
        }

        [TestMethod]
        public void GeometrySystem_Acquire_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRendererFrontend();
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

            uint expectedInternalId = 1;
            mockRenderer.OnLoadGeometry += (Geometry geometry) =>
            {
                geometry.UpdateInternalId(expectedInternalId); //set the id for being loaded
            };

            textureSystem.Init(mockRenderer);
            materialSystem.Init(mockRenderer);
            geometrySystem.Init(mockRenderer);

            var planeConfig = geometrySystem.GeneratePlaneConfig(5, 5, 1, 1, 1, 1, "test_plane", "test_material");
            var geometry = geometrySystem.AcquireFromConfig(planeConfig, true);

            Assert.IsNotNull(geometry);
            Assert.AreEqual(1, geometrySystem.GeometriesCount, "Expected the geometry to be saved.");
            Assert.AreEqual(expectedInternalId, geometry.InternalId, "Id should be generated properly.");
            Assert.AreEqual(0U, geometry.Generation, "Generation should be set.");

        }

        [TestMethod]
        public void GeometrySystem_Release_Test()
        {
            var loggerMock = new Mock<ILogger>();
            var mockRenderer = new MockRendererFrontend();
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

            uint expectedInternalId = 1;
            mockRenderer.OnLoadGeometry += (Geometry geometry) =>
            {
                geometry.UpdateInternalId(expectedInternalId); //set the id for being loaded
            };

            textureSystem.Init(mockRenderer);
            materialSystem.Init(mockRenderer);
            geometrySystem.Init(mockRenderer);

            var planeConfig = geometrySystem.GeneratePlaneConfig(5, 5, 1, 1, 1, 1, "test_plane", "test_material");
            var geometry = geometrySystem.AcquireFromConfig(planeConfig, true);

            geometrySystem.Release(geometry);

            Assert.AreEqual(0, geometrySystem.GeometriesCount, "Expected the geometry to be removed once released.");
            Assert.AreEqual(EntityIdService.INVALID_ID, geometry.Generation);

        }
    }
}