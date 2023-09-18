using Silk.NET.Maths;

namespace DragonFoxGameEngine.Core
{
    public readonly struct Mesh
    {
       // private readonly Vector3D[] vertices;

        //public Vector3D[] Vertices => vertices;
        public ushort[] Indices { get; }

        public Mesh(float[] vertices, ushort[] indices)
        {
            //Vertices = vertices;
            Indices = indices;
        }
    }
}
