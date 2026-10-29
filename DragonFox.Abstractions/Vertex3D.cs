namespace DragonFox.Abstractions;

public readonly struct Vertex3D : IEquatable<Vertex3D>
{
    //Use fields for marshaling
    private readonly Vector3 _position;
    private readonly Vector2 _textureCoordinate;

    public Vector3 Position => _position;
    public Vector2 TextureCoordinate => _textureCoordinate;

    public Vertex3D(Vector3 position, Vector2 textureCoordinate)
    {
        _position = position;
        _textureCoordinate = textureCoordinate;
    }
    
    public override string ToString()
    {
        return $"Vertex(Position:{Position},TextureCoordinate: {TextureCoordinate})";
    }
    
    public bool Equals(Vertex3D other)
    {
        return Position == other.Position
            && TextureCoordinate == other.TextureCoordinate;
    }

    public override bool Equals(object? obj)
    {
        if(ReferenceEquals(obj, null))
        {
            return true;
        }
        
        return
            obj is Vertex3D other &&
            Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, TextureCoordinate);
    }
    
    public static bool operator ==(Vertex3D left, Vertex3D right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vertex3D left, Vertex3D right)
    {
        return !(left == right);
    }
}