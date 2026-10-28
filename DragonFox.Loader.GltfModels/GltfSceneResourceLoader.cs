using DragonFox.Abstractions;
using DragonFox.Assets;
using SharpGLTF.Schema2;

namespace DragonFox.Loader.GltfModels;

/// <summary>
/// Resource Loader for Gltf scenes
/// </summary>
public class GltfSceneResourceLoader
{
    private readonly AssetFolder _assetFolder;
    
    public GltfSceneResourceLoader(AssetFolder assetFolder)
    {
        _assetFolder = assetFolder;
    }

    public SimpleMesh Load(AssetIdentifier assetId)
    {
        var fullAssetPath = Path.Combine(_assetFolder.AsString(), assetId.AssetPath.AsString());
        if (!File.Exists(fullAssetPath))
        {
            throw new FileNotFoundException(fullAssetPath);
        }
        var model = SharpGLTF.Schema2.ModelRoot.Load(fullAssetPath);

        var vertices = new List<Vertex3D>();
        var indices = new List<uint>();

        foreach (var mesh in model.LogicalMeshes)
        {
            foreach (var meshPrimitive in mesh.Primitives)
            {
                var positions = meshPrimitive.GetVertexAccessor("POSITION")?.AsVector3Array();
                var normals  = meshPrimitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var texCoord0s = meshPrimitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                
                var trigs = meshPrimitive.GetTriangleIndices();

                foreach (var valueTuple in trigs)
                {
                    indices.Add((uint)valueTuple.A);
                    indices.Add((uint)valueTuple.B);
                    indices.Add((uint)valueTuple.C);
                }
                
                var position = positions[meshPrimitive.LogicalIndex];
                var texCoord = texCoord0s[meshPrimitive.LogicalIndex];

                var vertex = new Vertex3D(
                    new Vector3(position.X, position.Y, position.Z),
                    new Vector3(texCoord.X, texCoord.Y, 0));
                
                vertices.Add(vertex);
            }
        }
        
        return new SimpleMesh(vertices, indices);
    }
}
