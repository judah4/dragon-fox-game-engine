using DragonFox.Abstractions;

namespace DragonFox.Assets;

public sealed class SimpleMesh
{
    public ICollection<Vertex3D> Vertices { get; }
    
    public ICollection<uint> Indicies { get; }
    
    public SimpleMesh(ICollection<Vertex3D> vertices, ICollection<uint> indicies)
    {
        Vertices = vertices;
        Indicies = indicies;
    }
}