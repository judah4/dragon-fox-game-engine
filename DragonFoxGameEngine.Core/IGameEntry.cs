using Silk.NET.Maths;

namespace DragonGameEngine.Core
{
    /// <summary>
    /// The base game entry
    /// </summary>
    public interface IGameEntry
    {
        void Initialize(GameApplication gameApp);
        void Update(double deltaTime);
        void Render(double deltaTime);
        void OnResize(Vector2D<uint> size);
        void Shutdown();
    }
}
