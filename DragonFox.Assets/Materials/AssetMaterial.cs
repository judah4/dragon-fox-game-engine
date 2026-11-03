namespace DragonFox.Assets.Materials;

/// <summary>
/// In-Memory material data
/// </summary>
public class AssetMaterial
{
    public MaterialName Name { get; }
    
    public AssetMaterial(MaterialName name)
    {
        Name = name;
    }

}