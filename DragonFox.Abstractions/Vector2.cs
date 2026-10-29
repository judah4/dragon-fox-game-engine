namespace DragonFox.Abstractions;

public readonly struct Vector2 : IEquatable<Vector2>
{
    public float X { get; }
    public float Y { get; }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }
    
    public override string ToString()
    {
        return $"({X},{Y})";
    }
    
    public bool Equals(Vector2 other)
    {
        return Math.Abs(X - other.X) < float.Epsilon
               && Math.Abs(X - other.X) < float.Epsilon;
    }

    public override bool Equals(object? obj)
    {
        if(ReferenceEquals(obj, null))
        {
            return true;
        }
        
        return
            obj is Vector2 other &&
            Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
    
    public static bool operator ==(Vector2 left, Vector2 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector2 left, Vector2 right)
    {
        return !(left == right);
    }
}