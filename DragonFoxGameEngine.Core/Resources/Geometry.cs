using DragonGameEngine.Core.Ecs;
using System;
using System.Collections.Generic;

namespace DragonGameEngine.Core.Resources
{
    /// <summary>
    /// Represents geometry in the world.
    /// Typically (but not always, depending on use) paired with a material.
    /// </summary>
    public sealed class Geometry
    {
        public const int NAME_MAX_LENGTH = 512;

        public uint Id { get; }

        public uint InternalId { get; private set; }

        public string Name { get; }

        public uint Generation { get; private set; }

        public Material Material { get; private set; }

        public Geometry(string name, Material material)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > NAME_MAX_LENGTH)
            {
                throw new ArgumentException($"Name should not be less than {NAME_MAX_LENGTH}", nameof(name));
            }

            Id = GetIdByName(name);
            Name = name;
            Generation = EntityIdService.INVALID_ID;
            InternalId = EntityIdService.INVALID_ID;
            Material = material;

        }

        public void UpdateMaterial(Material material)
        {
            Material = material;
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

        public static uint GetIdByName(string name)
        {
            return unchecked((uint)name.GetHashCode());
        }
    }
}
