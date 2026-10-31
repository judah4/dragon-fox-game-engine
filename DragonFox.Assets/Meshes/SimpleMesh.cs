using DragonFox.Abstractions;

namespace DragonFox.Assets.Meshes;

/// <summary>
/// Simple base level mesh
/// </summary>
public sealed class SimpleMesh
{
    public ICollection<Vertex3D> Vertices { get; }
    
    public ICollection<uint> Indices { get; }
    
    public SimpleMesh(ICollection<Vertex3D> vertices, ICollection<uint> indices)
    {
        Vertices = vertices;
        Indices = indices;
    }
}