namespace DragonFox.Assets;

/// <summary>
/// The base asset path
/// </summary>
public sealed class AssetFolder
{
    private readonly string _path;

    public AssetFolder(string path)
    {
        if (!Path.Exists(path))
        {
            throw new DirectoryNotFoundException($"Path not found: {path}");
        }
        
        _path = path;
    }

    public string AsString()
    {
        return _path;
    }
}