
namespace DragonFoxGameEngine.Core.Ecs
{
    /// <summary>
    /// Entity service to get new entity ids
    /// </summary>
    public interface IEntityIdService
    {
        /// <summary>
        /// Get the next entity Id, thread safe.
        /// </summary>
        /// <returns></returns>
        uint GetNextEntityId();
    }

    /// <summary>
    /// Entity service to get new entity ids
    /// </summary>
    public class EntityIdService : IEntityIdService
    {
        private readonly static object s_syncRoot = new object();
        private uint _nextEntityId = 1;

        public uint GetNextEntityId()
        {
            lock (s_syncRoot)
            {
                var entityId = _nextEntityId;
                _nextEntityId++;
                return entityId;
            }
        }
    }
}
