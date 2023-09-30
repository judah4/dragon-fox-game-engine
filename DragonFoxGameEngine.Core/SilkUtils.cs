using Silk.NET.Core;

namespace DragonFoxGameEngine.Core
{
    public static class SilkUtils
    {
        public static string Version32ToString(Version32 version)
        {
            return $"{version.Major}.{version.Minor}.{version.Patch}";
        }
    }
}
