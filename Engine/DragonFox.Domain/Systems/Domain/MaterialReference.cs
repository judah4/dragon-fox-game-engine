using DragonGameEngine.Core.Resources;
namespace DragonGameEngine.Core.Systems.Domain
{
    public struct MaterialReference
    {
        public ulong ReferenceCount { get; set; }
        public Material Handle { get; init; }
        public bool AutoRelease { get; init; }
    }
}
