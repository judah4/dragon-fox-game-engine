using Silk.NET.Maths;

namespace DragonGameEngine.Core.InitialPrototype
{
    public readonly struct Mesh
    {
        private readonly Vector3D<float>[] _vertices;

        public Vector3D<float>[] Vertices => _vertices;

        public ushort[] Indices { get; }

        public Mesh(Vector3D<float>[] vertices, ushort[] indices)
        {
            _vertices = vertices;
            Indices = indices;
        }
    }
}
