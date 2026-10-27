namespace DragonFox.Assets;

/// <summary>
/// The asset id and relative path
/// </summary>
/// <remarks>
/// The GUID should be generated and save somewhere. Build the asset database in memory for now.
/// </remarks>
public class AssetIdentifier
{
    public Guid Id { get; }
    
    public AssetPath AssetPath { get; }
    
    public AssetIdentifier(Guid id, AssetPath assetPath)
    {
        Id = id;
        AssetPath = assetPath;
    }
}