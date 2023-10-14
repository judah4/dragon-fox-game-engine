using DragonGameEngine.Core.Rendering;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace DragonGameEngine.Core
{
    /// <summary>
    /// The base game entry
    /// </summary>
    public interface IGameEntry
    {
        void Initialize(IWindow window, RendererFrontend renderer);
        void Update(double deltaTime);
        void Render(double deltaTime);
        void OnResize(Vector2D<uint> size);
        void Shutdown();
    }
}
