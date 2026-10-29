namespace DragonFox.Abstractions;

public readonly struct Vector3 : IEquatable<Vector3>
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    
    public override string ToString()
    {
        return $"({X},{Y},{Z})";
    }
    
    public bool Equals(Vector3 other)
    {
        return Math.Abs(X - other.X) < float.Epsilon
               && Math.Abs(X - other.X) < float.Epsilon
               && Math.Abs(Y - other.Y) < float.Epsilon;
    }

    public override bool Equals(object? obj)
    {
        if(ReferenceEquals(obj, null))
        {
            return true;
        }
        
        return
            obj is Vector3 other &&
            Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }
    
    public static bool operator ==(Vector3 left, Vector3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector3 left, Vector3 right)
    {
        return !(left == right);
    }
}