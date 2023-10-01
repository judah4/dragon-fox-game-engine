using DragonFoxGameEngine.Core;
using DragonFoxGameEngine.Core.Platforms;
using Silk.NET.Maths;

namespace GameEngine.Core.Tests
{
    public static class ApplicationConfigTestProvider
    {

        /// <summary>
        /// Create an Application Config for tests.
        /// </summary>
        /// <returns><see cref="ApplicationConfig"></see></returns>
        public static ApplicationConfig CreateTestConfig()
        {
            var config = new ApplicationConfig(
                ApplicationInfo.GetGameEngineName(),
                new Vector2D<int>(-1, -1),
                new Vector2D<int>(ApplicationConfig.WIDTH, ApplicationConfig.HEIGHT),
                true, //don't actually initialize a renderer for these tests.
                ApplicationConfig.GOOD_MAX_FPS,
                ApplicationConfig.GOOD_MAX_FPS);
            return config;
        }
    }
}
