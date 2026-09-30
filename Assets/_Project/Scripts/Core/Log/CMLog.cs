using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>Category-tagged logging: CMLog.Info("Possession", "msg") → "[Possession] msg".</summary>
    public static class CMLog
    {
        [HideInCallstack]
        public static void Info(string category, string message)
        {
            Debug.Log("[" + category + "] " + message);
        }

        [HideInCallstack]
        public static void Warn(string category, string message)
        {
            Debug.LogWarning("[" + category + "] " + message);
        }

        [HideInCallstack]
        public static void Error(string category, string message)
        {
            Debug.LogError("[" + category + "] " + message);
        }
    }
}
