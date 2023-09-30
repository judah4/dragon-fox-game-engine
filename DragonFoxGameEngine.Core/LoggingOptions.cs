using NReco.Logging.File;
using System;

namespace DragonFoxGameEngine.Core
{
    public static class LoggingOptions
    {
        public static string FormatLogMessage(LogMessage msg)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(DateTime.Now.ToString("o"));
            sb.Append(' ');
            sb.Append(msg.LogLevel);
            sb.Append(' ');
            sb.Append(msg.LogName);
            if(msg.EventId.Id != 0)
            {
                sb.Append(' ');
                sb.Append(msg.EventId.Id);
            }
            sb.Append(' ');
            sb.Append(msg.Message);
            if (msg.Exception != null)
            {
                sb.Append(' ');
                sb.Append(msg.Exception.ToString());
            }

            return sb.ToString();          
        }
    }
}
