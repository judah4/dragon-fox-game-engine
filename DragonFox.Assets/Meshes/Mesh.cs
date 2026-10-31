using DragonFox.Assets.Materials;

namespace DragonFox.Assets.Meshes;

/// <summary>
/// Mesh with sub meshes
/// </summary>
public sealed class Mesh
{
    public IReadOnlyDictionary<MaterialName, SimpleMesh> SubMeshes { get; }
    
    public Mesh(IReadOnlyDictionary<MaterialName, SimpleMesh> subMeshes)
    {
        SubMeshes = subMeshes;
    }
}