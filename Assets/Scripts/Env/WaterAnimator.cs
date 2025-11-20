using UnityEngine;

public class WaterAnimator : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private WaterTextureGenerator textureGenerator;
    [SerializeField] private SpriteRenderer waterRenderer;

    [Header("Animation Settings")]
    [SerializeField] private float framesPerSecond = 6f; // Unified speed
    [SerializeField] private bool animateOnStart = true;
    [SerializeField] private float startFrameOffset = 0f; // Start offset

    private Sprite[] frames;
    private float frameTimer = 0f;

    // Static timer for synchronizing all tiles
    private static float globalFrameTimer = 0f;
    private static int globalCurrentFrame = 0;

    void Awake()
    {
        if (waterRenderer == null)
        {
            waterRenderer = GetComponent<SpriteRenderer>();
        }
    }

    void Start()
    {
        if (textureGenerator == null)
        {
            textureGenerator = FindObjectOfType<WaterTextureGenerator>();
        }

        if (textureGenerator != null)
        {
            frames = textureGenerator.GetWaterSprites();
        }

        if (frames == null || frames.Length == 0)
        {
            DebugHelper.LogError($"[WaterAnimator] {gameObject.name}: No frames!");
            return;
        }

        if (waterRenderer == null)
        {
            DebugHelper.LogError($"[WaterAnimator] {gameObject.name}: No SpriteRenderer!");
            return;
        }

        // Apply start offset (for variation if needed)
        frameTimer = startFrameOffset;

        // Set first frame
        waterRenderer.sprite = frames[globalCurrentFrame % frames.Length];
    }

    void Update()
    {
        if (!animateOnStart || frames == null || frames.Length == 0 || waterRenderer == null)
            return;

        // Use GLOBAL timer for synchronization
        globalFrameTimer += Time.deltaTime;

        if (globalFrameTimer >= 1f / framesPerSecond)
        {
            globalFrameTimer = 0f;
            globalCurrentFrame = (globalCurrentFrame + 1) % frames.Length;
        }

        // All tiles show the same frame
        waterRenderer.sprite = frames[globalCurrentFrame];
    }

    public void SetFrameRate(float fps)
    {
        framesPerSecond = fps;
    }
}