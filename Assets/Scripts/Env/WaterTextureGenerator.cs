using UnityEngine;

public class WaterTextureGenerator : MonoBehaviour
{
    [Header("Texture Settings")]
    [SerializeField] private int textureWidth = 64;
    [SerializeField] private int textureHeight = 64;
    [SerializeField] private int pixelsPerUnit = 16;

    [Header("Water Colors")]
    [SerializeField] private Color deepWaterColor = new Color(0.1f, 0.4f, 0.7f, 1f);
    [SerializeField] private Color shallowWaterColor = new Color(0.3f, 0.6f, 0.9f, 1f);
    [SerializeField] private Color foamColor = new Color(0.8f, 0.9f, 1f, 0.5f);

    [Header("Wave Parameters")]
    [SerializeField] private float waveScale = 4f; // Reduced for smoothness
    [SerializeField] private float foamThreshold = 0.65f;
    [SerializeField] private int animationFrames = 8;

    private Sprite[] waterSprites;
    private bool isGenerated = false;

    void Awake()
    {
        GenerateWaterSprites();
    }

    [ContextMenu("Generate Textures")]
    public void GenerateWaterSprites()
    {
        DebugHelper.Log("[WaterGenerator] Starting tileable texture generation...");

        waterSprites = new Sprite[animationFrames];

        for (int frame = 0; frame < animationFrames; frame++)
        {
            float timeOffset = (float)frame / animationFrames;
            Texture2D texture = GenerateTileableWaterTexture(timeOffset);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect
            );

            sprite.name = $"Water_Frame_{frame}";
            waterSprites[frame] = sprite;
        }

        isGenerated = true;
        DebugHelper.Log($"Created {waterSprites.Length} tileable frames!");
    }

    // New method - generates seamless texture
    private Texture2D GenerateTileableWaterTexture(float timeOffset)
    {
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                // Normalized coordinates (0-1)
                float u = (float)x / textureWidth;
                float v = (float)y / textureHeight;

                // Tileable Perlin noise
                // Using sine/cosine for looping
                float angle = u * Mathf.PI * 2f;
                float angleV = v * Mathf.PI * 2f;

                float nx = Mathf.Cos(angle) * waveScale / (2f * Mathf.PI) + timeOffset * 2f;
                float ny = Mathf.Sin(angle) * waveScale / (2f * Mathf.PI) + timeOffset * 2f;
                float nz = Mathf.Cos(angleV) * waveScale / (2f * Mathf.PI);
                float nw = Mathf.Sin(angleV) * waveScale / (2f * Mathf.PI);

                // Two octaves of noise
                float noise1 = Mathf.PerlinNoise(nx, nz);
                float noise2 = Mathf.PerlinNoise(nx * 2f + timeOffset, nz * 2f) * 0.5f;

                float combinedNoise = (noise1 + noise2) / 1.5f;

                // Determine color
                Color pixelColor;

                if (combinedNoise > foamThreshold)
                {
                    float foamStrength = (combinedNoise - foamThreshold) / (1f - foamThreshold);
                    pixelColor = Color.Lerp(shallowWaterColor, foamColor, foamStrength);
                }
                else
                {
                    pixelColor = Color.Lerp(deepWaterColor, shallowWaterColor, combinedNoise / foamThreshold);
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        return texture;
    }

    public Sprite[] GetWaterSprites()
    {
        if (!isGenerated)
        {
            GenerateWaterSprites();
        }

        return waterSprites;
    }

    public bool IsGenerated() => isGenerated;
}