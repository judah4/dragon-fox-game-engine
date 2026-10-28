using System.Collections.Immutable;
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
                if (meshPrimitive.DrawPrimitiveType == PrimitiveType.POINTS
                    || meshPrimitive.DrawPrimitiveType == PrimitiveType.LINES
                    || meshPrimitive.DrawPrimitiveType == PrimitiveType.LINE_LOOP
                    || meshPrimitive.DrawPrimitiveType == PrimitiveType.LINE_STRIP)
                {
                    continue;
                }
                var positions = meshPrimitive.GetVertexAccessor("POSITION")?.AsVector3Array();
                if (positions.Count < 3)
                {
                    continue;
                }
                
                var normals  = meshPrimitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var texCoord0s = meshPrimitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                
                var primIndices = meshPrimitive.GetIndices();

                foreach (var index in primIndices)
                {
                    var position = positions[(int)index];
                    var texCoord = texCoord0s[(int)index];
                    
                    var vertex = new Vertex3D(
                        new Vector3(position.X, position.Y, position.Z),
                        new Vector2(texCoord.X, texCoord.Y));
                    
                    // index should be usable once the meshes are no longer being merged
                    indices.Add((uint)vertices.Count);
                    vertices.Add(vertex);
                }
            }
        }
        
        return new SimpleMesh(vertices.ToImmutableList(), indices.ToImmutableList());
    }
}
