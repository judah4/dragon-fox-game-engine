using DragonFox.Assets;
using glTFLoader;
using glTFLoader.Schema;

namespace DragonFox.Loader.GltfModels;

/// <summary>
/// Resource Loader for Gltf scenes
/// </summary>
public class GltfSceneResourceLoader
{
    private readonly AssetFolder _assetFolder;
    
    public GltfSceneResourceLoader(AssetFolder assetFolder)
    {
        _assetFolder = assetFolder;
    }

    public Gltf Load(AssetIdentifier assetId)
    {
        var fullAssetPath = Path.Combine(_assetFolder.AsString(), assetId.AssetPath.AsString());
        if (!File.Exists(fullAssetPath))
        {
            throw new FileNotFoundException(fullAssetPath);
        }
        var model = Interface.LoadModel(fullAssetPath);
        return model;
    }
}
