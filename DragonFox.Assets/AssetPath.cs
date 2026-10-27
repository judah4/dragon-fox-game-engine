namespace DragonFox.Assets;

/// <summary>
/// The relative path for an asset
/// </summary>
public sealed class AssetPath
{
    private readonly string _path;

    public AssetPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException($"'{nameof(path)}' cannot be null or empty.", nameof(path));
        }

        if (path.StartsWith("/"))
        {
            throw new ArgumentException($"'{nameof(path)}' must be relative from the asset folder.", nameof(path));
        }
        
        _path = path;
    }
    
    public string AsString()
    {
        return _path;
    }
}