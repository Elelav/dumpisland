using UnityEngine;
using System.Collections.Generic;

public class TextureDebugger : MonoBehaviour
{
    public static TextureDebugger Instance;

    [Header("Texture Statistics")]
    [SerializeField] private int totalTexturesCreated = 0;
    [SerializeField] private int currentTexturesInMemory = 0;

    private Dictionary<string, int> texturesBySource = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterTextureCreation(string source, int width, int height)
    {
        totalTexturesCreated++;
        currentTexturesInMemory++;

        string key = $"{source} ({width}x{height})";

        if (texturesBySource.ContainsKey(key))
        {
            texturesBySource[key]++;
        }
        else
        {
            texturesBySource[key] = 1;
        }

        Debug.LogWarning($"TEXTURE CREATED! Source: {source}, Size: {width}x{height}, Total created: {totalTexturesCreated}");
    }

    public void RegisterTextureDestroyed(string source)
    {
        currentTexturesInMemory--;
        Debug.Log($"Texture destroyed: {source}");
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 200, 400, 400));
        GUILayout.Label($"<b>TEXTURES:</b>");
        GUILayout.Label($"Total created: {totalTexturesCreated}");
        GUILayout.Label($"In memory: {currentTexturesInMemory}");
        GUILayout.Label("");

        foreach (var kvp in texturesBySource)
        {
            GUILayout.Label($"{kvp.Key}: {kvp.Value} pcs");
        }

        GUILayout.EndArea();
    }

    [ContextMenu("Show Statistics")]
    public void ShowStats()
    {
        Debug.Log("=== TEXTURE STATISTICS ===");
        Debug.Log($"Total created: {totalTexturesCreated}");
        Debug.Log($"In memory: {currentTexturesInMemory}");

        foreach (var kvp in texturesBySource)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value}");
        }
    }
}