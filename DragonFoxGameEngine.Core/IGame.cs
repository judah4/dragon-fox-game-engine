using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace DragonFoxGameEngine.Core
{
    /// <summary>
    /// The basic game state
    /// </summary>
    public interface IGame
    {
        void Initialize(IWindow window);
        void Update(double deltaTime);
        void Render(double deltaTime);
        void OnResize(Vector2D<int> size);
    }
}
