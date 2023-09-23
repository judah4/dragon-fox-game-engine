using DragonFoxGameEngine.Core.Ecs.Components;
using Microsoft.Extensions.Logging;
using Svelto.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonFoxGameEngine.Core.Ecs.Systems
{
    /// <summary>
    ///     This is the common pattern to declare Svelto Exclusive Groups (usually split by composition root)
    /// </summary>
    public static class ExclusiveGroups
    {
        public static ExclusiveGroup GroupEngine = new ExclusiveGroup("Engine");
        public static ExclusiveGroup Group1 = new ExclusiveGroup();
    }

    public class CounterSystemEngine : IUpdateEngine, IQueryingEntitiesEngine
    {
        //extra entity functions
        readonly IEntityFunctions _entityFunctions;
        readonly ILogger _logger;

        public CounterSystemEngine(IEntityFunctions entityFunctions, ILogger logger)
        {
            _entityFunctions = entityFunctions;
            _logger = logger;
        }

        public EntitiesDB? entitiesDB { get; set; }

        public string name => nameof(CounterSystemEngine);

        public void Ready() { }

        public void Step(in double deltaTime)
        {
            var (components, entityIDs, count) = entitiesDB!.QueryEntities<CounterEntityComponent>(ExclusiveGroups.GroupEngine);

            uint entityID;
            for (var i = 0; i < count; i++)
            {
                components[i].counter++;
                entityID = entityIDs[i];
                //_logger.LogDebug($"{entityID} - {components[i].counter}");
            }
        }
    }
}

