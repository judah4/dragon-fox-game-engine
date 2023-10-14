using DragonGameEngine.Core;
using Silk.NET.Maths;

namespace DragonGameEngine.Core.Platforms
{
    public readonly struct ApplicationConfig
    {
        public const int WIDTH = 1280;
        public const int HEIGHT = 720;
        public const int GOOD_MAX_FPS = 1024;

        public readonly Vector2D<int> StartingPos;
        public readonly Vector2D<int> StartingSize;

        public readonly string Title;

        /// <summary>
        /// Headless mode to not initialize the rendering engine.
        /// </summary>
        public readonly bool HeadlessMode;

        public readonly double UpdatesPerSecond;
        public readonly double FramesPerSecond;

        public ApplicationConfig(string title, Vector2D<int> startingPos, Vector2D<int> startingSize, bool headlessMode, double updatesPerSecond, double framesPerSecond)
        {
            Title = title;
            if (string.IsNullOrEmpty(title))
            {
                Title = DefaultGameName();
            }
            StartingPos = startingPos;
            StartingSize = startingSize;
            HeadlessMode = headlessMode;
            UpdatesPerSecond = updatesPerSecond;
            FramesPerSecond = framesPerSecond;
        }

        public ApplicationConfig()
        {
            StartingPos = new Vector2D<int>(-1, -1);
            StartingSize = new Vector2D<int>(WIDTH, HEIGHT);
            Title = DefaultGameName();
            HeadlessMode = false;
            UpdatesPerSecond = 0;
            FramesPerSecond = 0;
        }

        public string DefaultGameName()
        {
            return $"{ApplicationInfo.GetGameEngineName()} Game";
        }
    }
}
