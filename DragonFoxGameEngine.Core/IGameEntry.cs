using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace DragonFoxGameEngine.Core
{
    /// <summary>
    /// The base game entry
    /// </summary>
    public interface IGameEntry
    {
        void Initialize(IWindow window);
        void Update(double deltaTime);
        void Render(double deltaTime);
        void OnResize(Vector2D<int> size);
    }
}
