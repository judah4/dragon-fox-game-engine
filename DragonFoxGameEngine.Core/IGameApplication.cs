using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace DragonGameEngine.Core;

public interface IGameApplication
{
        IWindow Window { get; }

        void Init();
        
        void Run();

        void Shutdown();

        void SetRendererCameraView(Matrix4X4<float> cameraView);
        
        void UpdateWindowTitle(string title);
        
        /// <summary>
        /// Cycle the square's texture for texture loading
        /// </summary>
        void CycleTestTexture();

}