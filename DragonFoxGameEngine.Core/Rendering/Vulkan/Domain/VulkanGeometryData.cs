using DragonGameEngine.Core.Ecs;
using System;
namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    /// <summary>
    /// Internal buffer data for geometry. This data gets loaded directly into a buffer
    /// </summary>
    public readonly struct VulkanGeometryData
    {
        //TODO: make configurable
        /// <summary>
        /// Max number of simultaneously uploaded geometries
        /// </summary>
        public const int MAX_GEOMENTRY_COUNT = 4096;

        public uint Id { get; }
        public uint Generation { get; }
        public uint VertexCount { get; }
        public uint VertexSize { get; }
        public ulong VertexBufferOffset { get; }
        public uint IndexCount { get; }
        public uint IndexSize { get; }
        public ulong IndexBufferOffset { get; }

        public VulkanGeometryData(uint id, uint generation, uint vertexCount, uint vertexSize, ulong vertexBufferOffset, uint indexCount, uint indexSize, ulong indexBufferOffset)
        {
            Id = id;
            Generation = generation;
            VertexCount = vertexCount;
            VertexSize = vertexSize;
            VertexBufferOffset = vertexBufferOffset;
            IndexCount = indexCount;
            IndexSize = indexSize;
            IndexBufferOffset = indexBufferOffset;
        }

        public VulkanGeometryData(uint id)
        {
            Id = id;
            Generation = EntityIdService.INVALID_ID;
        }
    }
}
