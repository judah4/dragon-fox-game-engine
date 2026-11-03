using System.Collections.Immutable;
using DragonFox.Abstractions;
using DragonFox.Assets;
using DragonFox.Assets.Materials;
using DragonFox.Assets.Meshes;
using SharpGLTF.Schema2;
using Mesh = DragonFox.Assets.Meshes.Mesh;
using MeshPrimitive = DragonFox.Assets.Meshes.MeshPrimitive;

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

    public FullModelAsset Load(AssetIdentifier assetId)
    {
        var fullAssetPath = Path.Combine(_assetFolder.AsString(), assetId.AssetPath.AsString());
        if (!File.Exists(fullAssetPath))
        {
            throw new FileNotFoundException(fullAssetPath);
        }

        var model = SharpGLTF.Schema2.ModelRoot.Load(fullAssetPath);

        var nodes = new List<ModelNode>();
        
        foreach (var node in model.DefaultScene.VisualChildren)
        {
            nodes.Add(LoadNode(assetId, node));
        }

        var materials = model.LogicalMaterials.Select(material => new AssetMaterial(GetMaterialName(material, assetId)))
            .ToImmutableList();

        var textures = model.LogicalTextures.Select(texture => new AssetTexture())
            .ToImmutableList();

        return new FullModelAsset(nodes, materials, textures);
    }

    // TODO: prepend Asset on my types
    private ModelNode LoadNode(AssetIdentifier assetId, SharpGLTF.Schema2.Node node)
    {
        var mesh = LoadMesh(assetId, node.Mesh);
        var children = new List<ModelNode>();

        foreach (var childNode in node.VisualChildren)
        {
            children.Add(LoadNode(assetId, childNode));
        }

        return new ModelNode(children, [mesh]);
    }

    private Mesh LoadMesh(AssetIdentifier assetId, SharpGLTF.Schema2.Mesh mesh)
    {
        var subMeshes = new List<MeshPrimitive>();
        
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
                    
            var vertices = new Vertex3D[positions.Count];
            var indices = primIndices.ToImmutableList();

            for (int index = 0; index < positions.Count; index++)
            {
                var position = positions[index];
                var texCoord = texCoord0s[index];
                
                var vertex = new Vertex3D(
                    new Vector3(position.X, position.Y, position.Z),
                    new Vector2(texCoord.X, texCoord.Y));
                // TODO: Add normal and other vector 2s and 3s as attributes
                // TODO: Split vertices for array alignment of vectors?
                
                vertices[index] = vertex;
            }

            var simpleMesh = new SimpleMesh(vertices, indices);
            var materialName = GetMaterialName(meshPrimitive.Material, assetId);
            subMeshes.Add(new MeshPrimitive(simpleMesh, materialName));
        }
        
        return new Mesh(subMeshes);
    }

    private MaterialName GetMaterialName(Material material, AssetIdentifier assetId)
    {
        return new MaterialName($"{assetId.Id}#{material.LogicalIndex}");
    }
}

public sealed class ModelNode
{
    public IEnumerable<ModelNode> Children { get; }
    
    public IEnumerable<Mesh> Meshes { get; }

    public ModelNode(IEnumerable<ModelNode> children, IEnumerable<Mesh> meshes)
    {
        Children = [.. children];
        Meshes = [.. meshes];
    }
}

public sealed class FullModelAsset
{
    public IEnumerable<ModelNode> Nodes { get; }
    
    public IEnumerable<AssetMaterial> Materials { get; }
    
    public IEnumerable<AssetTexture> Textures { get; }
    
    public FullModelAsset(IEnumerable<ModelNode> nodes, IEnumerable<AssetMaterial> materials, IEnumerable<AssetTexture> textures)
    {
        Nodes = nodes;
        Materials = materials;
        Textures = textures;
    }

}
