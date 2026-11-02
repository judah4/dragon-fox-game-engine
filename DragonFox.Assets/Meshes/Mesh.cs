using DragonFox.Assets.Materials;

namespace DragonFox.Assets.Meshes;

/// <summary>
/// Mesh with sub meshes
/// </summary>
public sealed class Mesh
{
    public ICollection<MeshPrimitive> SubMeshes { get; }
    
    public Mesh(ICollection<MeshPrimitive> subMeshes)
    {
        SubMeshes = [.. subMeshes];
    }
}