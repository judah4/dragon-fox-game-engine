using DragonFox.Assets.Materials;

namespace DragonFox.Assets.Meshes;

/// <summary>
/// Simple Mesh for loading meshes and materials
/// </summary>
public class MeshPrimitive {
    public SimpleMesh Mesh { get; }
    public MaterialName MaterialName { get; }

    public MeshPrimitive(SimpleMesh mesh, MaterialName materialName)
    {
        Mesh = mesh;
        MaterialName = materialName;
    }
}