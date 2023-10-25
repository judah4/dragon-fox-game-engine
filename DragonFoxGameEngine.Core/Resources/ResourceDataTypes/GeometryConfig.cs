using DragonGameEngine.Core.Maths;
using DragonGameEngine.Core.Resources;
using System.Collections.Generic;

namespace DragonGameEngine.Core.Resources.ResourceDataTypes
{
    public readonly struct GeometryConfig
    {
        public uint Id { get; }
        public string Name { get; init; }
        public Vertex3d[] Vertices { get; init; }
        public uint[] Indices { get; init; }
        public string MaterialName { get; init; }

        public GeometryConfig(string name, Vertex3d[] vertices, uint[] indices, string materialName)
        {
            Id = Geometry.GetIdByName(name);
            Name = name;
            Vertices = vertices;
            Indices = indices;
            MaterialName = materialName;
        }
    }
}
