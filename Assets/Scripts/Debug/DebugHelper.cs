using UnityEngine;

public static class DebugHelper
{
    // Enable/disable Debug here
    public const bool ENABLE_DEBUG = false; // Set to false for release!

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOGS")]
    public static void Log(object message)
    {
        if (ENABLE_DEBUG)
        {
            DebugHelper.Log(message);
        }
    }

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOGS")]
    public static void LogWarning(object message)
    {
        if (ENABLE_DEBUG)
        {
            DebugHelper.LogWarning(message);
        }
    }

    [System.Diagnostics.Conditional("ENABLE_DEBUG_LOGS")]
    public static void LogError(object message)
    {
        // Errors are always shown!
        DebugHelper.LogError(message);
    }
}