using UnityEngine;

public class MemoryManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float cleanupInterval = 60f;
    [SerializeField] private bool clearConsole = true; // New

    private float lastCleanup = 0f;

    void Update()
    {
        if (Time.time - lastCleanup >= cleanupInterval)
        {
            lastCleanup = Time.time;
            CleanupMemory();
        }
    }

    private void CleanupMemory()
    {
        Debug.Log("Cleaning memory...");

        // Unload unused assets
        Resources.UnloadUnusedAssets();

        // Garbage collection
        System.GC.Collect();

        // Clear console
        if (clearConsole)
        {
            ClearConsole();
        }

        Debug.Log("Memory cleanup complete!");
    }

    // Clear console using reflection
    private void ClearConsole()
    {
#if UNITY_EDITOR
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
#endif
    }
}