using DragonGameEngine.Core.Resources;
using Silk.NET.Maths;

namespace DragonGameEngine.Core.Systems.Domain
{
    public readonly struct MaterialConfig
    {
        public string Name { get; init; }
        public bool AutoRelease { get; init; }
        public Vector4D<float> DiffuseColor { get; init; }
        public string DiffuseMapName { get; init; }

        public MaterialConfig(string name, bool autoRelease, Vector4D<float> diffuseColor, string diffuseMapName)
        {
            Name = name;
            AutoRelease = autoRelease;
            DiffuseColor = diffuseColor;
            DiffuseMapName = diffuseMapName;
        }
    }
}
