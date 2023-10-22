using DragonGameEngine.Core.Ecs;
using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Resources.ResourceDataTypes;
using DragonGameEngine.Core.Systems.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace DragonGameEngine.Core.Systems
{
    public sealed class GeometrySystem
    {
        /// <summary>
        /// TODO: setup default primatives for Square, Circle, and Capsule.
        /// </summary>
        public const string DEFAULT_GEOMETRY_NAME = "default";

        private readonly ILogger _logger;
        private readonly GeometrySystemConfig _config;
        private readonly MaterialSystem _materialSystem;
        private readonly ResourceSystem _resourceSystem;
        private readonly Geometry _defaultGeometry;
        private readonly Dictionary<uint, GeometryReference> _geometries;

        private IRendererFrontend? _renderer;

        /// <summary>
        /// Number of materials loaded and referenced.
        /// </summary>
        public int GeometriesCount => _geometries.Count;

        public GeometrySystem(ILogger logger, GeometrySystemConfig config, MaterialSystem materialSystem, ResourceSystem resourceSystem)
        {
            _logger = logger;
            _config = config;
            _materialSystem = materialSystem;
            _geometries = new Dictionary<uint, GeometryReference>((int)config.MaxGeometryCount);
            _defaultGeometry = new Geometry(DEFAULT_GEOMETRY_NAME, materialSystem.GetDefaultMaterial());
            _resourceSystem = resourceSystem;
        }

        public void Init(IRendererFrontend renderer)
        {
            _renderer = renderer;
            CreateDefaultGeometry();

            _logger.LogInformation("Geometry System initialized");
        }

        public void Shutdown()
        {
            //nothing really to do.
            _geometries.Clear();

            _logger.LogInformation("Geometry System shutdown");
        }
        public Geometry Acquire(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Equals(DEFAULT_GEOMETRY_NAME))
            {
                return _defaultGeometry;
            }

            var resource = _resourceSystem.Load(name, ResourceType.StaticMesh);
            var config = (GeometryConfig)resource.Data;

            //Now Acquire from loaded config
            var geometry = AcquireFromConfig(config, true);

            _resourceSystem.Unload(resource);
            return geometry;
        }

        public Geometry AcquireById(uint id)
        {
            if(id == EntityIdService.INVALID_ID)
            {
                //oof
                throw new EngineException("Cannot acquire geometry with an an invalid id.");
            }
            if(_geometries.TryGetValue(id, out var geometryRef))
            {
                //ref up
                geometryRef.ReferenceCount++;
                _geometries[id] = geometryRef;

                return geometryRef.GeometryHandle;
            }

            //NOTE: Should return default geometry instead?
            throw new EngineException("Cannot load geometry with that id. Try loading it first.");
        }

        public Geometry AcquireFromConfig(GeometryConfig config, bool autoRelease)
        {
            if (config.Name.Equals(DEFAULT_GEOMETRY_NAME))
            {
                return _defaultGeometry;
            }

            //Find the existing reference
            if (!_geometries.TryGetValue(config.Id, out var geometryRef))
            {
                geometryRef = new GeometryReference()
                {
                    ReferenceCount = 0,
                    AutoRelease = autoRelease, //set only the first time it's loaded
                    GeometryHandle = new Geometry(config.Name, _materialSystem.GetDefaultMaterial()),
                };
                _geometries.Add(config.Id, geometryRef);
            }
            geometryRef.ReferenceCount++;

            //Load geometry
            CreateGeometry(config, geometryRef.GeometryHandle);

            _geometries[config.Id] = geometryRef;

            return geometryRef.GeometryHandle;
        }

        /// <summary>
        /// Releases the geometry from memory
        /// </summary>
        /// <param name="geometry"></param>
        public void Release(Geometry geometry) 
        {
            if (geometry.Name.Equals(DEFAULT_GEOMETRY_NAME))
            {
                return;
            }
            var id = geometry.Id;
            //Find the existing material reference
            if (!_geometries.TryGetValue(id, out var geometryRef))
            {
                _logger.LogWarning("Tried to release a non-existing geometry {name}", geometry.Name);
                return;
            }
            if(geometryRef.GeometryHandle.Id != id)
            {
                throw new EngineException("Geometry id mismatch. Check registation logic,as this should never occur.");
            }
            if (geometryRef.ReferenceCount > 0)
            {
                geometryRef.ReferenceCount--;
            }
            else
            {
                _logger.LogWarning("Geometry {name} was already at 0 references. Cleaning up", geometry.Name);
            }

            _geometries[id] = geometryRef;

            if (geometryRef.ReferenceCount == 0 && geometryRef.AutoRelease)
            {
                //release geometry
                DestroyGeometry(geometryRef.GeometryHandle);
                _geometries.Remove(id);

                //TODO: pool thse geometries classes later so they don't hit the GC.
            }
        }

        /// <summary>
        /// Get default geometry
        /// </summary>
        /// <returns></returns>
        public Geometry GetDefaultGeometry()
        {
            return _defaultGeometry;
        }

        /// <summary>
        /// Generate configuration for plane geometries given the provided parameters.
        /// </summary>
        /// <remarks>
        /// vertex and index arrays are dynamically allocated so watch for GC hits and probably should not be considered production code.
        /// </remarks>
        /// <param name="width">The overall width of the plane. Must be non-zero.</param>
        /// <param name="height">The overall height of the plane. Must be non-zero.</param>
        /// <param name="xSegmentCount">The number of segments along the x-axis in the plane. Must be non-zero.</param>
        /// <param name="ySegmentCount">The number of segments along the y-axis in the plane. Must be non-zero.</param>
        /// <param name="tileX">The number of times the texture should tile across the plan on the x-axis. Must be non-zero.</param>
        /// <param name="tileY">The number of times the texture should tile across the plan on the y-axis. Must be non-zero.</param>
        /// <param name="name">The name of the generated geometry</param>
        /// <param name="materialName">The name of the material to be used.</param>
        /// <returns>Geometry</returns>
        public GeometryConfig GeneratePlaneConfig(float width, float height, uint xSegmentCount, uint ySegmentCount, float tileX, float tileY, string name, string materialName)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = Guid.NewGuid().ToString();
            }
            if (string.IsNullOrEmpty(materialName))
            {
                materialName = MaterialSystem.DEFAULT_MATERIAL_NAME;
            }

            if (width <= 0)
            {
                _logger.LogWarning("Width must be nonzero. Defaulting to one.");
                width = 1.0f;
            }
            if (height <= 0)
            {
                _logger.LogWarning("Height must be nonzero. Defaulting to one.");
                height = 1.0f;
            }
            if (xSegmentCount < 1)
            {
                _logger.LogWarning("xSegmentCount must be a positive number. Defaulting to one.");
                xSegmentCount = 1;
            }
            if (ySegmentCount < 1)
            {
                _logger.LogWarning("ySegmentCount must be a positive number. Defaulting to one.");
                ySegmentCount = 1;
            }

            if (tileX < 0)
            {
                _logger.LogWarning("tileX must be nonzero. Defaulting to one.");
                tileX = 1.0f;
            }
            if (tileY < 0)
            {
                _logger.LogWarning("tileY must be nonzero. Defaulting to one.");
                tileY = 1.0f;
            }

            var vertexCount = xSegmentCount * ySegmentCount * 4;  // 4 verts per segment
            var vertices = new Vertex3d[vertexCount];
            var indexCount = xSegmentCount * ySegmentCount * 6;  // 6 indices per segment
            var indices = new uint[indexCount];

            // TODO: This generates extra vertices, but we can always deduplicate them later.
            float seg_width = width / xSegmentCount;
            float seg_height = height / ySegmentCount;
            float half_width = width * 0.5f;
            float half_height = height * 0.5f;
            for (uint y = 0; y < ySegmentCount; ++y)
            {
                for (uint x = 0; x < xSegmentCount; ++x)
                {
                    // Generate vertices
                    float minX = (x * seg_width) - half_width;
                    float minY = (y * seg_height) - half_height;
                    float maxX = minX + seg_width;
                    float maxY = minY + seg_height;
                    float minUvx = (x / (float)xSegmentCount) * tileX;
                    float minUvy = (y / (float)ySegmentCount) * tileY;
                    float maxUvx = ((x + 1) / (float)xSegmentCount) * tileX;
                    float maxUvy = ((y + 1) / (float)ySegmentCount) * tileY;

                    uint vOffset = ((y * xSegmentCount) + x) * 4;
                    var v0Pos = Vector3D<float>.One;
                    var v0TextCoord = Vector2D<float>.Zero;
                    var v1Pos = Vector3D<float>.One;
                    var v1TextCoord = Vector2D<float>.Zero;
                    var v2Pos = Vector3D<float>.One;
                    var v2TextCoord = Vector2D<float>.Zero;
                    var v3Pos = Vector3D<float>.One;
                    var v3TextCoord = Vector2D<float>.Zero;

                    v0Pos.X = minX;
                    v0Pos.Y = minY;
                    v0TextCoord.X = minUvx;
                    v0TextCoord.Y = minUvy;

                    v1Pos.X = maxX;
                    v1Pos.Y = maxY;
                    v1TextCoord.X = maxUvx;
                    v1TextCoord.Y = maxUvy;

                    v2Pos.X = minX;
                    v2Pos.Y = maxY;
                    v2TextCoord.X = minUvx;
                    v2TextCoord.Y = maxUvy;

                    v3Pos.X = maxX;
                    v3Pos.Y = minY;
                    v3TextCoord.X = maxUvx;
                    v3TextCoord.Y = minUvy;

                    vertices[vOffset + 0] = new Vertex3d(v0Pos, v0TextCoord);
                    vertices[vOffset + 1] = new Vertex3d(v1Pos, v1TextCoord);
                    vertices[vOffset + 2] = new Vertex3d(v2Pos, v2TextCoord);
                    vertices[vOffset + 3] = new Vertex3d(v3Pos, v3TextCoord);

                    // Generate indices
                    uint iOffset = ((y * xSegmentCount) + x) * 6;
                    indices[iOffset + 0] = vOffset + 0;
                    indices[iOffset + 1] = vOffset + 1;
                    indices[iOffset + 2] = vOffset + 2;
                    indices[iOffset + 3] = vOffset + 0;
                    indices[iOffset + 4] = vOffset + 3;
                    indices[iOffset + 5] = vOffset + 1;
                }
            }

            GeometryConfig config = new GeometryConfig(name, vertices, indices, materialName);

            return config;
        }

        private void CreateGeometry(GeometryConfig config, Geometry geometry)
        {
            if(_renderer == null)
            {
                throw new EngineException("Geometry System is not initialized!");
            }
            //send the geometry to the renderer to be uploaded to the GPU

            _renderer.LoadGeometry(geometry, config.Vertices, config.Indices);

            //acquire the material
            if(!string.IsNullOrEmpty(config.MaterialName))
            {
                try
                {
                    var material = _materialSystem.Acquire(config.MaterialName);
                    geometry.UpdateMaterial(material);
                }
                catch (ResourceException e)
                {
                    _logger.LogError(e, "Unable to load material {matName} for geometry {geoName}, using default. {eMessage}", e.Message, config.MaterialName, geometry.Name);
                }
            }

            var generation = 0U;
            if(geometry.Generation != EntityIdService.INVALID_ID)
            {
                generation++;
            }
            geometry.UpdateGeneration(generation);
        }

        private void DestroyGeometry(Geometry geometry)
        {
            if (_renderer == null)
            {
                throw new EngineException("Geometry System is not initialized!");
            }
            geometry.ResetGeneration();
            geometry.UpdateInternalId(EntityIdService.INVALID_ID);

            _renderer.DestroyGeometry(geometry);

            _materialSystem.Release(geometry.Material.Name);
            //change back to default material just in case
            geometry.UpdateMaterial(_materialSystem.GetDefaultMaterial());
        }

        private void CreateDefaultGeometry()
        {
            if (_renderer == null)
            {
                throw new EngineException("Geometry System is not initialized! This should only be called in init!");
            }

            float trigSize = 5f;

            var verts = new Vertex3d[]
            {
                new Vertex3d(new Vector3D<float>(-0.5f, -0.5f, 0) * trigSize, new Vector2D<float>(0,0)),
                new Vertex3d(new Vector3D<float>(0.5f, 0.5f, 0) * trigSize, new Vector2D<float>(1f,1f)),
                new Vertex3d(new Vector3D<float>(-0.5f, 0.5f, 0) * trigSize, new Vector2D<float>(0,1f)),
                new Vertex3d(new Vector3D<float>(0.5f, -0.5f, 0) * trigSize, new Vector2D<float>(1f,0)),
            };

            // 2_____1  0,1_____1,1
            //  |  /|      |  /|
            //  | / |      | / |
            //  |/  |      |/  | 
            // 0-----3  0,0-----1,0   

            var indices = new uint[]
            {
                0,1,2,
                0,3,1,
            };

            //Send the geometry off to the renderer to be uploaded to the GPU
            _renderer.LoadGeometry(_defaultGeometry, verts, indices);

            //already has the default material so no need to call it here.
        }
    }
}
