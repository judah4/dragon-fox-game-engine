using Silk.NET.Maths;

namespace DragonFoxGameEngine.Core.Rendering
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Should be 256 bytes in size for nVidia
    /// </remarks>
    public struct GlobalUniformObject
    {
        public Matrix4X4<float> Projection; //64 bytes
        public Matrix4X4<float> View; //64 bytes
        public Matrix4X4<float> Reserved1; //64 bytes, reserved for future
        public Matrix4X4<float> Reserved2; //64 bytes, reserved for future
    }
}
