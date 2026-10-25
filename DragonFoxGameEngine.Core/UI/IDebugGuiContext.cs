using System;
using DragonGameEngine.Core.Rendering;

namespace DragonGameEngine.Core.UI;

public interface IDebugGuiContext : IDisposable
{
    void Init();

    void WindowResized(int width, int height);
    
    void Update(float deltaTime);

    GeometryRenderData? Render();
}