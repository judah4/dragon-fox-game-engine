using System;
using System.Linq;
using System.Reflection;

namespace DragonGameEngine.Core
{
    public static class ApplicationInfo
    {
        public static readonly string[] GAME_ENGINE_NAMES = new[]
        {
            "Coffee Dragon Engine",
            "Dragon Fox Engine",
            "Bookwyrm Engine",
            "Kitsune Engine", // weakest so far
            "Untitled Dragon Engine",
            "Dragon Weaver Engine",
            "Kitsune Weaver Engine",
        };

        public static readonly Version GameVersion;

        public static readonly Version EngineVersion;

        private static int _nameIndex;

        static ApplicationInfo()
        {
            //for now, game and engine version are the same
            var version = typeof(ApplicationInfo).Assembly?.GetName().Version ?? new Version(1, 0);
            GameVersion = version;
            EngineVersion = version;

            var entryInterface = typeof(IGameEntry);
            var entryType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => p.IsClass && entryInterface.IsAssignableFrom(p))
                .FirstOrDefault();

            if (entryType == null)
                return;

            var gameVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0);
            GameVersion = gameVersion;

            _nameIndex = EngineRandom.Random.Next(GAME_ENGINE_NAMES.Length);
        }

        public static string GetGameEngineName()
        {
            return GAME_ENGINE_NAMES[EngineRandom.Random.Next(_nameIndex)];
        }
    }
}
