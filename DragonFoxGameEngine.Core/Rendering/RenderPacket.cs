
namespace DragonFoxGameEngine.Core.Rendering
{
    public struct RenderPacket
    {
        public readonly double DeltaTime;

        public RenderPacket(double deltaTime)
        {
            DeltaTime = deltaTime;
        }
    }
}
