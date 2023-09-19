using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonFoxGameEngine.Core
{
    public static class ApplicationInfo
    {
        public static readonly Version GameVersion;

        public static readonly Version EngineVersion;

        static ApplicationInfo()
        {
            //for now, game and engine version are the same
            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0);
            GameVersion = version;
            EngineVersion = version;
        }

    }
}
