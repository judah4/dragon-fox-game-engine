using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Resources.ResourceDataTypes;
using DragonGameEngine.Core.Systems;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace DragonGameEngine.Core.UI;

public sealed class DragonFoxImGuiContext : IDisposable
{
    private readonly IntPtr _context;
    private int _windowWidth;
    private int _windowHeight;
    private Vector2 _scaleFactor = Vector2.One;
    private readonly GeometrySystem _geometrySystem;
    
    public DragonFoxImGuiContext(int width, int height, GeometrySystem geometrySystem)
    {
        _windowWidth = width;
        _windowHeight = height;
        _geometrySystem = geometrySystem;

        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard |
                          ImGuiConfigFlags.DockingEnable;
        io.Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;
    }
    
    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }
    public void Update(float deltaSeconds)
    {
        ImGui.Render();
        
        ImGui.Text("Hello World!");
        
        SetPerFrameImGuiData(deltaSeconds);
        
        ImGui.NewFrame();
    }
    
    public IEnumerable<GeometryRenderData> Render()
    {
        //https://github.com/ImGuiNET/ImGui.NET/blob/master/src/ImGui.NET.SampleProgram/ImGuiController.cs
        var drawData = ImGui.GetDrawData();

        var builder = new List<GeometryRenderData>();
        
        uint totalVBSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        uint totalIBSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));

        var geometry = _geometrySystem.AcquireFromConfig(new GeometryConfig("ImGui", vertices, indices, "default2d")); // Get from material system.
    }

    public void Dispose()
    {
        ImGui.DestroyContext(_context);
    }
    
    /// <summary>
    /// Sets per-frame data based on the associated window.
    /// This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new Vector2(
            _windowWidth / _scaleFactor.X,
            _windowHeight / _scaleFactor.Y);
        io.DisplayFramebufferScale = _scaleFactor;
        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }
}