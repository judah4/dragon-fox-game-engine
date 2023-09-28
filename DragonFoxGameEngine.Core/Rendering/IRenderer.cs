using Silk.NET.Maths;

namespace DragonFoxGameEngine.Core.Rendering
{
    public interface IRenderer
    {
        public void Shutdown();

        public void Resized(Vector2D<uint> size);

        public bool BeginFrame(double deltaTime);

        public bool EndFrame(double deltaTime);
    }
}
