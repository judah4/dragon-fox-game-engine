using DragonGameEngine.Core.Resources;

namespace DragonGameEngine.Core.Systems.Domain
{
    public struct GeometryReference
    {
        public ulong ReferenceCount { get; set; }
        public Geometry GeometryHandle { get; init; }
        public bool AutoRelease { get; init; }
    }
}
