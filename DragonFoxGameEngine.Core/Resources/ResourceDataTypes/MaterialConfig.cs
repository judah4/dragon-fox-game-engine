using Silk.NET.Maths;

namespace DragonGameEngine.Core.Resources.ResourceDataTypes
{
    public readonly struct MaterialConfig
    {
        public MaterialType MaterialType { get; init; }
        public string Name { get; init; }
        public bool AutoRelease { get; init; }
        public Vector4D<float> DiffuseColor { get; init; }
        public string DiffuseMapName { get; init; }

        public MaterialConfig(MaterialType materialType, string name, bool autoRelease, Vector4D<float> diffuseColor, string diffuseMapName)
        {
            MaterialType = materialType;
            Name = name;
            AutoRelease = autoRelease;
            DiffuseColor = diffuseColor;
            DiffuseMapName = diffuseMapName;
        }
    }
}
