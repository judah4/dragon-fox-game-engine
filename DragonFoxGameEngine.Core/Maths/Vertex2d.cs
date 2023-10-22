using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Runtime.InteropServices;

namespace DragonGameEngine.Core.Maths
{
    public readonly struct Vertex2d
    {
        //Use fields for marshaling
        private readonly Vector2D<float> _position;
        //public readonly Vector3D<float> _color;
        public readonly Vector2D<float> _textureCoordinate;

        public Vector2D<float> Position => _position;
        //public Vector3D<float> Color => _color;
        public Vector2D<float> TextureCoordinate => _textureCoordinate;

        public Vertex2d(Vector2D<float> position, Vector2D<float> textureCoordinate)
        {
            _position = position;
            _textureCoordinate = textureCoordinate;
        }

        public static VertexInputAttributeDescription[] GetAttributeDescriptions()
        {
            var attributeDescriptions = new[]
            {
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 0,
                    Format = Format.R32G32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex2d>(nameof(_position)),
                },
                //new VertexInputAttributeDescription()
                //{
                //    Binding = 0,
                //    Location = 1,
                //    Format = Format.R32G32B32Sfloat,
                //    Offset = (uint)Marshal.OffsetOf<Vertex3d>(nameof(Color)),
                //},
                new VertexInputAttributeDescription()
                {
                    Binding = 0,
                    Location = 1,
                    Format = Format.R32G32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex2d>(nameof(_textureCoordinate)),
                }
            };

            return attributeDescriptions;
        }
    }
}
