using Silk.NET.Maths;

namespace DragonGameEngine.Core.Rendering
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Should be 256 bytes in size for nVidia
    /// </remarks>
    public struct GlobalUniformObject
    {
        public Matrix4X4<float> Projection { get; init; } //64 bytes
        public Matrix4X4<float> View { get; init; } //64 bytes
        public Matrix4X4<float> Reserved1 { get; init; } //64 bytes, reserved for future
        public Matrix4X4<float> Reserved2 { get; init; } //64 bytes, reserved for future
    }
}
