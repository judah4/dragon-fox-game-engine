using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems.Domain;
using Microsoft.Extensions.Logging;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using System.Collections.Generic;
using System.IO;

namespace DragonGameEngine.Core.Systems.ResourceLoaders
{
    public sealed class StaticMeshResourceLoader : IResourceLoader
    {
        public ResourceType ResourceType => ResourceType.StaticMesh;

        public string TypePath => "";


        private readonly ILogger _logger;
        private readonly string _basePath;

        public StaticMeshResourceLoader(ILogger logger, string basePath)
        {
            _logger = logger;
            _basePath = basePath;
        }

        public Resource Load(string name)
        {
            return LoadModel(name);
        }

        public void Unload(Resource resource)
        {
            resource.Unload();
        }

        private unsafe Scene* OpenScene(Assimp assimp, string name, string filePath)
        {
            Scene* scene;
            try
            {
                scene = assimp.ImportFile(filePath, (uint)PostProcessPreset.TargetRealTimeMaximumQuality);
            }
            catch (DirectoryNotFoundException e)
            {
                throw new ResourceException(name, e.Message, e);
            }
            catch (FileNotFoundException e)
            {
                throw new ResourceException(name, e.Message, e);
            }

            if(scene == null)
            {
                throw new ResourceException(name, "File did not load!");
            }

            return scene;
        }

        private unsafe Resource LoadModel(string name)
        {
            var filePath = Path.Combine(_basePath, TypePath, name);

            using var assimp = Assimp.GetApi();
            
            var scene = OpenScene(assimp, name, filePath);

            var vertexMap = new Dictionary<Vertex3d, uint>();
            var vertices = new List<Vertex3d>();
            var indices = new List<uint>();

            VisitSceneNode(scene->MRootNode);

            assimp.ReleaseImport(scene);

            var geometryConfig = new GeometryConfig(name, vertices.ToArray(), indices.ToArray(), name);
            return new Resource(ResourceType, name, filePath, (ulong)geometryConfig.Vertices.LongLength, geometryConfig);

            void VisitSceneNode(Node* node)
            {
                for (int m = 0; m < node->MNumMeshes; m++)
                {
                    var mesh = scene->MMeshes[node->MMeshes[m]];

                    for (int f = 0; f < mesh->MNumFaces; f++)
                    {
                        var face = mesh->MFaces[f];

                        for (int i = 0; i < face.MNumIndices; i++)
                        {
                            uint index = face.MIndices[i];

                            var position = mesh->MVertices[index];
                            var texture = System.Numerics.Vector3.Zero;
                            var textureCoords = mesh->MTextureCoords;
                            if (textureCoords.Element0 != default)
                            {
                                texture = mesh->MTextureCoords[0][(int)index];
                            }

                            var vertex = new Vertex3d(
                                new Vector3D<float>(position.X, position.Y, position.Z),
                                //Flip Y for OBJ in Vulkan
                                //new Vector2D<float>(texture.X, 1.0f - texture.Y));
                                new Vector2D<float>(texture.X, texture.Y));

                            if (vertexMap.TryGetValue(vertex, out var meshIndex))
                            {
                                indices.Add(meshIndex);
                            }
                            else
                            {
                                indices.Add((uint)vertices.Count);
                                vertexMap[vertex] = (uint)vertices.Count;
                                vertices.Add(vertex);
                            }
                        }
                    }
                }

                for (int c = 0; c < node->MNumChildren; c++)
                {
                    VisitSceneNode(node->MChildren[c]);
                }
            }
        }
    }
}
