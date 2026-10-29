using Silk.NET.Maths;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    /// <summary>
    /// 
    /// </summary>
    public struct VulkanUiShaderInstanceUniformObject
    {
        public Vector4D<float> DiffuseColor { get; init; } //16 bytes
        public Vector4D<float> Reserved1 { get; init; } //16 bytes, reserved for future
        public Vector4D<float> Reserved2 { get; init; } //16 bytes, reserved for future
        public Vector4D<float> Reserved3 { get; init; } //16 bytes, reserved for future

        public Matrix4X4<float> MReserved0 { get; init; } //64 bytes
        public Matrix4X4<float> MReserved1 { get; init; } //64 bytes
        public Matrix4X4<float> MReserved2 { get; init; } //64 bytes
    }
}
