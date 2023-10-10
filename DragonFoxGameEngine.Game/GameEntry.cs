using DragonGameEngine.Core;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace DragonFoxGameEngine.Game
{
    public sealed class GameEntry : IGameEntry
    {
        /// <summary>
        /// Set this name once a game is in progress
        /// </summary>
        public const string GAME_NAME = "";
        private readonly ILogger _logger;

        public GameEntry(ILogger logger)
        {
            _logger = logger;
        }

        public void Initialize(IWindow window)
        {
            _logger.LogDebug("Game initialized!");
        }

        public void Update(double deltaTime)
        {
        }

        public void Render(double deltaTime)
        {
        }

        public void OnResize(Vector2D<uint> size)
        {
            _logger.LogDebug("Game resized!");

        }

        public void Shutdown()
        {
            _logger.LogDebug("Game shutdown.");
        }

    }
}