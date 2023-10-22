using Silk.NET.Maths;

namespace DragonGameEngine.Core.Rendering.Vulkan.Domain.Shaders
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Should be 256 bytes in size for nVidia
    /// </remarks>
    public struct VulkanMaterialShaderGlobalUniformObject
    {
        public Matrix4X4<float> Projection { get; init; } //64 bytes
        public Matrix4X4<float> View { get; init; } //64 bytes
        public Matrix4X4<float> Reserved1 { get; init; } //64 bytes, reserved for future
        public Matrix4X4<float> Reserved2 { get; init; } //64 bytes, reserved for future
    }
}
