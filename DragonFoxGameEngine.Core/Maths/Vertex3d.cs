using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace DragonGameEngine.Core.Maths
{
    public struct Vertex3d
    {
        public Vector3D<float> Position;
        //public Vector3D<float> Color;
        //public Vector2D<float> TextureCoordinate;

        public Vertex3d(Vector3D<float> position)
        {
            Position = position;
        }

        public static VertexInputAttributeDescription[] GetAttributeDescriptions()
        {
            var attributeDescriptions = new[]
            {
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 0,
                    Format = Format.R32G32B32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex3d>(nameof(Position)),
                },
                //new VertexInputAttributeDescription()
                //{
                //    Binding = 0,
                //    Location = 1,
                //    Format = Format.R32G32B32Sfloat,
                //    Offset = (uint)Marshal.OffsetOf<Vertex3d>(nameof(Color)),
                //},
                //new VertexInputAttributeDescription()
                //{
                //    Binding = 0,
                //    Location = 2,
                //    Format = Format.R32G32Sfloat,
                //    Offset = (uint)Marshal.OffsetOf<Vertex3d>(nameof(TextureCoordinate)),
                //}
            };

            return attributeDescriptions;
        }
    }
}
