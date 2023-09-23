using DragonFoxGameEngine.Core.Ecs.Components;
using Microsoft.Extensions.Logging;
using Svelto.ECS;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonFoxGameEngine.Core.Ecs.Systems
{
    public class AddCounterSystemEngine : IUpdateEngine
    {
        //extra entity functions
        readonly IEntityFactory _entityFactory;
        readonly IEntityIdService _entityService;
        readonly ILogger _logger;
        private float _timer;

        public AddCounterSystemEngine(IEntityFactory entityFactory, IEntityIdService entityService, ILogger logger)
        {
            _entityFactory = entityFactory;
            _entityService = entityService;
            _logger = logger;
        }

        public EntitiesDB? entitiesDB { get; set; }

        public string name => nameof(CounterSystemEngine);


        public void Ready() { }

        public void Step(in double deltaTime)
        {
            _timer += (float)deltaTime;
            if (_timer < 1)
                return;

            _timer -= 1;

            var entityId = _entityService.GetNextEntityId();

            var init = _entityFactory.BuildEntity<SimpleEntityDescriptor>(new EGID(entityId, ExclusiveGroups.GroupEngine));
            init.Init(new CounterEntityComponent()
            {
                counter = (int)entityId,
            });

            _logger.LogDebug($"Created Entity {entityId}");
        }
    }
}

