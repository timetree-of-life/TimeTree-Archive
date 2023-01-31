using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTreeShared
{
    public interface ILogAware
    {
        StringBuilder logger { get; set; }
    }

    public static class LogAwareExtensions 
    {
        public static void LogDebug(this ILogAware logAware, string format, params object[] args)
        {
            // write to log here
            logAware.logger.AppendLine(args[0].ToString());
        }

        public static void LogWarn(this ILogAware logAware, string format, params object[] args)
        {
            // write warning
        }

        public static void LogInfo(this ILogAware logAware, string format, params object[] args)
        {
            // write info-level warning
            logAware.logger.AppendLine(args[0].ToString());
        }

        public static void LogError(this ILogAware logAware, string format, params object[] args)
        {
            // write error-level warning
        }

        public static void LogFatal(this ILogAware logAware, string format, params object[] args)
        {
            // write fatal-level warning
        }
    }
}
