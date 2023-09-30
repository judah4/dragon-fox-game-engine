using System.Text.RegularExpressions;

namespace Foxis.Library
{
    public static class StringUtils
    {
        /// <summary>
        /// Format CamelCased strings so that they split as expected
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ToStringNameFormat(this string val)
        {
            var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
            return r.Replace(val, " ");
        }
    }
}
