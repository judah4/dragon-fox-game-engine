using System;
using System.Collections.Generic;
using System.Numerics;
using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Rendering;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Resources.ResourceDataTypes;
using DragonGameEngine.Core.Systems;
using ImGuiNET;
using Silk.NET.Maths;

namespace DragonGameEngine.Core.UI;

public sealed class DragonFoxImGuiContext : IDebugGuiContext
{
    private readonly IntPtr _fontAtlasID = 1;
    private readonly IntPtr _context;

    private int _windowWidth;
    private int _windowHeight;
    private Vector2 _scaleFactor = Vector2.One;
    
    private readonly GeometrySystem _geometrySystem;
    private readonly TextureSystem _textureSystem;
    
    public DragonFoxImGuiContext(int width, int height, GeometrySystem geometrySystem, TextureSystem textureSystem)
    {
        _windowWidth = width;
        _windowHeight = height;
        _geometrySystem = geometrySystem;
        _textureSystem = textureSystem;

        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard |
                          ImGuiConfigFlags.DockingEnable;
        io.Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;
    }

    public void Init()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        // Build
        IntPtr pixels;
        int width, height, bytesPerPixel;
        io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height, out bytesPerPixel);
        // Store our identifier
        io.Fonts.SetTexID(_fontAtlasID);

        // Should be 4 bytes per pixel for channels
        var channels = (byte)bytesPerPixel;

        io.Fonts.ClearTexData();

        var texture = new Texture(
            "ImGui.NETFontTexture",
            new Vector2D<uint>((uint)width, (uint)height),
            channels,
            true,
            new object(),
            0);
        
        var pixelCount = width * height;
        
        byte[] pixelsArray = new byte[pixelCount * channels];
        Array.Fill<byte>(pixelsArray, 255); //set full
        
        _textureSystem.CreateInMemoryTexture(texture, pixelsArray);
    }
    
    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }
    public void Update(float deltaSeconds)
    {
        SetPerFrameImGuiData(deltaSeconds);
        
        ImGui.NewFrame();
        
        ImGui.Text("Hello World!");
        
        ImGui.Render();
        
        ImGui.EndFrame();
    }

    public GeometryRenderData? Render()
    {
        //https://github.com/ImGuiNET/ImGui.NET/blob/master/src/ImGui.NET.SampleProgram/ImGuiController.cs
        var drawData = ImGui.GetDrawData();
        
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements = 0;

        if (drawData.CmdListsCount == 0)
        {
            return null;
        }

        var vertices = new List<Vertex3d>();
        var indices = new List<uint>();
        
        // Setup orthographic projection matrix into our constant buffer
        ImGuiIOPtr io = ImGui.GetIO();

        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        // Render verts
        int vtx_offset = 0;
        int idx_offset = 0;
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = drawData.CmdLists[n];
            for (int vertI = 0; vertI < cmd_list.VtxBuffer.Size; vertI++)
            {
                ImDrawVertPtr vert = cmd_list.VtxBuffer[vertI];
                vertices.Add(new Vertex3d(new Vector3D<float>(vert.pos.X, vert.pos.Y, 0),
                    new Vector2D<float>(vert.uv.X, vert.uv.Y)));
            }
            
            for (int indI = 0; indI < cmd_list.IdxBuffer.Size; indI++)
            {
                ushort idx = cmd_list.IdxBuffer[indI];
                indices.Add(idx);
            }
            vtx_offset += cmd_list.VtxBuffer.Size;
            idx_offset += cmd_list.IdxBuffer.Size;
        }

        // TODO: Why is this geometry corrupting the others?
        //var matName = "imgui_ui_mat";
        //var geometry = _geometrySystem.AcquireFromConfig(
        //    new GeometryConfig("ImGui", vertices.ToArray(), indices.ToArray(), matName),
        //    true); // Get from material system.
        
        var uiConfig = new GeometryConfig("ImGui", vertices.ToArray(), indices.ToArray(), "test_ui_material");
        var geometry = _geometrySystem.AcquireFromConfig(uiConfig, true); // Get from material system.

        // get texture command data
//
        //return new GeometryRenderData()
        //{
        //    Geometry = geometry,
        //    Model = Matrix4X4.CreateTranslation(new Vector3D<float>(0, 0f, 0)),
        //};
        return null;
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