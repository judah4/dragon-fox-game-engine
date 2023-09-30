
using System;
using System.Linq;
using System.Reflection;

namespace DragonFoxGameEngine.Core
{
    public static class ApplicationInfo
    {
        public const string GAME_ENGINE_NAME = "Dragon Fox Game Engine";

        public static readonly Version GameVersion;

        public static readonly Version EngineVersion;

        static ApplicationInfo()
        {
            //for now, game and engine version are the same
            var version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0);
            GameVersion = version;
            EngineVersion = version;

            var entryInterface = typeof(IGameEntry);
            var entryType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => p.IsClass && entryInterface.IsAssignableFrom(p))
                .FirstOrDefault();

            if(entryType == null)
                return;

            var gameVersion = entryType.Assembly.GetName().Version ?? new Version(1, 0);
            GameVersion = gameVersion;
        }

    }
}
