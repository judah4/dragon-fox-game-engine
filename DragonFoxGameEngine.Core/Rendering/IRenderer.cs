using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace DragonFoxGameEngine.Core.Rendering
{
    public interface IRenderer
    {
        public void Shutdown()
        {

        }

        public void Resized(Vector2D<int> size)
        {

        }

        public void BeginFrame(double deltaTime)
        {

        }

        public void EndFrame(double deltaTime)
        {

        }
    }
}
