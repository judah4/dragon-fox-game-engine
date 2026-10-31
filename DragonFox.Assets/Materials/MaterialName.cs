namespace DragonFox.Assets.Materials;

public sealed class MaterialName  : IEquatable<MaterialName>
{
    private readonly string _name;
    
    public MaterialName(string name)
    {
        _name = name;
    }
    
    public string AsString()
    {
        return _name;
    }
    
    public override string ToString()
    {
        return _name;
    }
    
    public bool Equals(MaterialName? other)
    {
        if(ReferenceEquals(other, null))
        {
            return false;
        }
        
        return _name == other._name;
    }

    public override bool Equals(object? obj)
    {
        if(ReferenceEquals(obj, null))
        {
            return false;
        }
        
        return
            obj is MaterialName other &&
            Equals(other);
    }

    public override int GetHashCode()
    {
        return _name.GetHashCode();
    }
    
    public static bool operator ==(MaterialName left, MaterialName right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MaterialName left, MaterialName right)
    {
        return !(left == right);
    }
}