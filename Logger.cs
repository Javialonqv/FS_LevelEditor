using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_LevelEditor
{
    public static class Logger
    {
        public static void Log(object message)
        {
            Melon<Core>.Logger.Msg(message);
        }
        public static void DebugLog(object message)
        {
#if DEBUG
            Melon<Core>.Logger.Msg(message);
#endif
        }

        public static void Warning(object message)
        {
            Melon<Core>.Logger.Warning(message);
        }
        public static void DebugWarning(object message)
        {
#if DEBUG
            Melon<Core>.Logger.Warning(message);
#endif
        }

        public static void Error(object message)
        {
            Melon<Core>.Logger.Error(message);
        }
        public static void DebugError(object message)
        {
#if DEBUG
            Melon<Core>.Logger.Error(message);
#endif
        }
    }
}
