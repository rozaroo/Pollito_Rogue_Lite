using UnityEngine;

namespace Custom.Logger
{
    public static class CustomLogger
    {
        [System.Diagnostics.Conditional("ENABLE_LOGS")]
        public static void Log(object message)
        {
            Debug.Log(message);
        }
    }
}