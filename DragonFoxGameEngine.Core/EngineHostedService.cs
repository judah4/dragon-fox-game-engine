using DragonFoxGameEngine.Core.Components;
using DragonFoxGameEngine.Core.Systems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Svelto.ECS.Schedulers;
using Svelto.ECS;

namespace DragonFoxGameEngine.Core
{
    public class EngineHostedService : IHostedService
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        IHostApplicationLifetime _appLifetime;
        private readonly EnginesRoot _enginesRoot;
        private SimpleEntitiesSubmissionScheduler _entitiesSubmissionScheduler;
        private IEntityIdService _entityIdService;

        public EngineHostedService(
            IConfiguration configuration,
            ILogger<EngineHostedService> logger,
            IHostApplicationLifetime appLifetime)
        {
            _configuration = configuration;
            _logger = logger;
            _appLifetime = appLifetime;

            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);


            _entitiesSubmissionScheduler = new SimpleEntitiesSubmissionScheduler();
            //Build Svelto Entities and Engines container, called EnginesRoot
            _enginesRoot = new EnginesRoot(_entitiesSubmissionScheduler);
            _entityIdService = new EntityIdService();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("1. StartAsync has been called.");

            _logger.LogInformation($"Dragon Fox Game Engine {ApplicationInfo.EngineVersion}");

            Task.Run(() =>
            {
                //this takes some time to pull hardware info so it's best to unload to another thread.
                _logger.LogInformation(HardwareStats.HardwareInfo());
            }).ConfigureAwait(true);

            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _logger.LogInformation("2. OnStarted has been called.");

            var entityFactory = _enginesRoot.GenerateEntityFactory();
            //the entity functions allows other operations on entities, like remove and swap
            var entityFunctions = _enginesRoot.GenerateEntityFunctions();

            var systemEnginesGroup = new SystemEnginesGroup();
            //Add an Engine to the enginesRoot to manage the SimpleEntities
            var behaviourForEntityClassEngine = new CounterSystemEngine(entityFunctions, _logger);
            systemEnginesGroup.Add(behaviourForEntityClassEngine);
            _enginesRoot.AddEngine(behaviourForEntityClassEngine);
            var addCounterEngine = new AddCounterSystemEngine(entityFactory, _entityIdService, _logger);
            systemEnginesGroup.Add(addCounterEngine);
            _enginesRoot.AddEngine(addCounterEngine);

            //build a new Entity with ID 0 in group0
            entityFactory.BuildEntity<SimpleEntityDescriptor>(new EGID(0, ExclusiveGroups.GroupEngine));

            //submit the previously built entities to the Svelto database
            _entitiesSubmissionScheduler.SubmitEntities();

            var app = new HelloTriangleApplication(_logger, systemEnginesGroup, _entitiesSubmissionScheduler);
            app.Run();
            _appLifetime.StopApplication();
        }

        private void OnStopping()
        {
            _logger.LogInformation("3. OnStopping has been called.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("4. StopAsync has been called.");

            return Task.CompletedTask;
        }

    }
}