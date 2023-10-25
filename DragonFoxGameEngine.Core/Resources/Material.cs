using DragonGameEngine.Core.Ecs;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace DragonGameEngine.Core.Resources
{
    public sealed class Material
    {
        public const int NAME_MAX_LENGTH = 512;

        public MaterialType MaterialType { get; }

        public uint Id { get; }

        public string Name { get; }

        public uint InternalId { get; private set; }

        public uint Generation { get; private set; }

        public Vector2D<uint> Size { get; private set; }
        public Vector4D<float> DiffuseColor { get; private set; }
        public TextureMap DiffuseMap { get; private set; }

        public Material(MaterialType materialType, string name)
        {
            if (name.Length > NAME_MAX_LENGTH)
            {
                throw new ArgumentException($"Name should not be less than {NAME_MAX_LENGTH}", nameof(name));
            }

            MaterialType = materialType;
            Id = unchecked((uint)name.GetHashCode());
            Name = name;
            Generation = EntityIdService.INVALID_ID;
            InternalId = EntityIdService.INVALID_ID;
        }

        public void UpdateMetaData(Vector4D<float> diffuseColor, TextureMap diffuseMap)
        {
            DiffuseColor = diffuseColor;
            DiffuseMap = diffuseMap;
        }

        public void ResetGeneration()
        {
            Generation = EntityIdService.INVALID_ID;
        }

        public void UpdateGeneration(uint generation)
        {
            Generation = generation;
        }

        public void UpdateInternalId(uint internalId)
        {
            InternalId = internalId;
        }
    }
}
