﻿using DragonGameEngine.Core.Ecs;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace DragonGameEngine.Core.Resources
{
    public sealed class Material
    {
        public const int NAME_MAX_LENGTH = 512;

        public uint Id { get; }

        public string Name { get; }

        public uint Generation { get; private set; }


        public uint InternalId { get; }

        public Vector2D<uint> Size { get; private set; }
        public Vector4D<float> DiffuseColor { get; private set; }
        public TextureMap DiffuseMap { get; private set; }

        public Material(string name)
        {
            if (name.Length > NAME_MAX_LENGTH)
            {
                throw new ArgumentException(nameof(name), $"Name should not be less than {NAME_MAX_LENGTH}");
            }

            Id = unchecked((uint)name.GetHashCode());
            Name = name;
            Generation = EntityIdService.INVALID_ID;

        }

    }
}
