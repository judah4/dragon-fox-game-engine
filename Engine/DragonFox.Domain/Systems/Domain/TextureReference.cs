using DragonGameEngine.Core.Resources;
namespace DragonGameEngine.Core.Systems.Domain
{
    public struct TextureReference
    {
        public ulong ReferenceCount { get; set; }
        public Texture TextureHandle { get; init; }
        public bool AutoRelease { get; init; }
    }
}
