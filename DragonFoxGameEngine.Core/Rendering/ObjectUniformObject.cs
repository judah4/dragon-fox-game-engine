using Silk.NET.Maths;

namespace DragonGameEngine.Core.Rendering
{
    /// <summary>
    /// 
    /// </summary>
    public struct ObjectUniformObject
    {
        public Vector4D<float> DiffuseColor; //16 bytes
        public Vector4D<float> Reserved1; //16 bytes, reserved for future
        public Vector4D<float> Reserved2; //16 bytes, reserved for future
        public Vector4D<float> Reserved3; //16 bytes, reserved for future
    }
}
