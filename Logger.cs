using System.Diagnostics;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace FS_LevelEditor
{
    public static class Logger
    {
        public static bool IncomingErrorIsPassive = false;

        public static void Log(object message)
        {
            Debug.Log(message);
        }
        public static void DebugLog(object message)
        {
#if DEBUG
            Debug.Log("[DEBUG] " + message);
#endif
        }

        public static void Warning(object message)
        {
            Debug.LogWarning(message);
        }
        public static void DebugWarning(object message)
        {
#if DEBUG
            Debug.LogWarning("[DEBUG] " + message);
#endif
        }

        public static void Error(object message, bool passive = false)
        {
            IncomingErrorIsPassive = passive;

            // Capture the stack trace this way so it also gets the calling functions and all.
            string stackTrace = new StackTrace(1, true).ToString(); // "1" to skip this (Logger.Error) function call frame, and only include the CALLING function.

            Debug.LogError($"{message}\n{stackTrace}");
        }
        public static void DebugError(object message, bool passive = false)
        {
            IncomingErrorIsPassive = passive;

#if DEBUG
            // Capture the stack trace this way so it also gets the calling functions and all.
            string stackTrace = new StackTrace(1, true).ToString(); // "1" to skip this (Logger.Error) function call frame, and only include the CALLING function.

            Debug.LogError($"[DEBUG] {message}\n{stackTrace}");
#endif
        }
    }
}
