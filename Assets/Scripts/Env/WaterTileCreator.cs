using UnityEngine;

public class WaterTileCreator : MonoBehaviour
{
    [Header("Area Settings")]
    [SerializeField] private Vector2 areaSize = new Vector2(100f, 100f);
    [SerializeField] private Vector2 tileSize = new Vector2(4f, 4f);

    [Header("Generator")]
    [SerializeField] private WaterTextureGenerator waterGenerator;

    [Header("Layers")]
    [SerializeField] private string sortingLayer = "Background";
    [SerializeField] private int orderInLayer = -10;

    void Start()
    {
        if (transform.childCount == 0)
        {
            CreateWaterTiles();
        }
    }

    [ContextMenu("Create Water")]
    public void CreateWaterTiles()
    {
        DebugHelper.Log("[WaterTileCreator] Creating water...");

        // Clear old water
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        if (waterGenerator == null)
        {
            waterGenerator = FindObjectOfType<WaterTextureGenerator>();
        }

        if (waterGenerator == null)
        {
            DebugHelper.LogError("WaterTextureGenerator not found!");
            return;
        }

        if (!waterGenerator.IsGenerated())
        {
            waterGenerator.GenerateWaterSprites();
        }

        Vector2 startPos = (Vector2)transform.position - areaSize / 2f;

        int tilesX = Mathf.CeilToInt(areaSize.x / tileSize.x);
        int tilesY = Mathf.CeilToInt(areaSize.y / tileSize.y);

        for (int y = 0; y < tilesY; y++)
        {
            for (int x = 0; x < tilesX; x++)
            {
                Vector2 tilePos = startPos + new Vector2(x * tileSize.x, y * tileSize.y);

                GameObject tile = new GameObject($"WaterTile_{x}_{y}");
                tile.transform.SetParent(transform);
                tile.transform.position = tilePos;
                tile.transform.localScale = tileSize;

                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = sortingLayer;
                sr.sortingOrder = orderInLayer;
                sr.color = Color.white;

                WaterAnimator animator = tile.AddComponent<WaterAnimator>();

                // Set dependencies via reflection
                var generatorField = typeof(WaterAnimator).GetField("textureGenerator",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var rendererField = typeof(WaterAnimator).GetField("waterRenderer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                generatorField?.SetValue(animator, waterGenerator);
                rendererField?.SetValue(animator, sr);

                // All tiles with the same speed (synchronized)
                // Removed random speed!
            }
        }

        DebugHelper.Log($"Created {tilesX * tilesY} synchronized tiles!");
    }
}