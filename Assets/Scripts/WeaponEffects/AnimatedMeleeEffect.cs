using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedMeleeEffect : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float fps = 12f;
    [SerializeField] private bool loop = false;
    [SerializeField] private bool destroyOnComplete = true;

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private float frameDuration;
    private Color tintColor = Color.white;
    private Vector3 targetScale;
    private bool isInitialized = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!isInitialized || frames == null || frames.Length == 0) return;

        frameTimer += Time.deltaTime;

        if (frameTimer >= frameDuration)
        {
            frameTimer = 0f;
            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                if (loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    if (destroyOnComplete)
                    {
                        Destroy(gameObject);
                    }
                    return;
                }
            }

            UpdateSprite();
        }

        // Smooth scaling at start (optional)
        if (currentFrame < 2)
        {
            float t = (currentFrame + (frameTimer / frameDuration)) / 2f;
            transform.localScale = Vector3.Lerp(targetScale * 0.7f, targetScale, t);
        }
    }

    /// <summary>
    /// Initialize effect
    /// </summary>
    /// <param name="animationFrames">Animation sprite array</param>
    /// <param name="position">Effect position</param>
    /// <param name="direction">Direction (for rotation)</param>
    /// <param name="color">Tint color</param>
    /// <param name="size">Size (scale)</param>
    /// <param name="animationSpeed">Animation FPS</param>
    public void Initialize(Sprite[] animationFrames, Vector3 position, Vector2 direction, Color color, float size, float animationSpeed = 12f, SpriteDirection baseDirection = SpriteDirection.South)
    {
        if (animationFrames == null || animationFrames.Length == 0)
        {
            DebugHelper.LogWarning("AnimatedMeleeEffect: No animation frames!");
            Destroy(gameObject);
            return;
        }

        frames = animationFrames;
        fps = animationSpeed;
        frameDuration = 1f / fps;
        tintColor = color;
        targetScale = Vector3.one * size;

        // Set position
        transform.position = position;

        // Fixed: Rotate considering base sprite direction
        if (direction.magnitude > 0.01f)
        {
            // Calculate attack direction angle
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Adjust for base sprite direction
            float baseAngle = 0f;
            switch (baseDirection)
            {
                case SpriteDirection.North:
                    baseAngle = 90f;  // Sprite faces up
                    break;
                case SpriteDirection.South:
                    baseAngle = -90f; // Sprite faces down
                    break;
                case SpriteDirection.East:
                    baseAngle = 0f;   // Sprite faces right
                    break;
                case SpriteDirection.West:
                    baseAngle = 180f; // Sprite faces left
                    break;
            }

            float finalAngle = targetAngle - baseAngle;
            transform.rotation = Quaternion.Euler(0, 0, finalAngle);

            DebugHelper.Log($"Rotation: target={targetAngle:F1}°, base={baseAngle:F1}°, final={finalAngle:F1}°");
        }

        // Initial scale
        transform.localScale = targetScale * 0.7f;

        // Configure SpriteRenderer
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = frames[0];
            spriteRenderer.color = tintColor;
            spriteRenderer.sortingLayerName = "Enemies";
            spriteRenderer.sortingOrder = 10;
        }

        currentFrame = 0;
        frameTimer = 0f;
        isInitialized = true;

        DebugHelper.Log($"AnimatedMeleeEffect: {frames.Length} frames, FPS={fps}, size={size}, baseDir={baseDirection}");
    }

    private void UpdateSprite()
    {
        if (spriteRenderer != null && currentFrame < frames.Length)
        {
            spriteRenderer.sprite = frames[currentFrame];
        }
    }

    /// <summary>
    /// For use without direction (e.g., circular effects)
    /// </summary>
    public void Initialize(Sprite[] animationFrames, Vector3 position, Color color, float size, float animationSpeed = 12f, SpriteDirection baseDirection = SpriteDirection.South)
    {
        Initialize(animationFrames, position, Vector2.right, color, size, animationSpeed, baseDirection);
    }

    /// <summary>
    /// Initialize with NON-UNIFORM scale (for Rectangle attacks)
    /// </summary>
    public void InitializeRectangle(Sprite[] animationFrames, Vector3 position, Vector2 direction, Color color, Vector2 size, float animationSpeed = 12f, SpriteDirection baseDirection = SpriteDirection.South)
    {
        if (animationFrames == null || animationFrames.Length == 0)
        {
            DebugHelper.LogWarning("AnimatedMeleeEffect: No animation frames!");
            Destroy(gameObject);
            return;
        }

        frames = animationFrames;
        fps = animationSpeed;
        frameDuration = 1f / fps;
        tintColor = color;

        // Calculate scale considering original sprite size!
        Sprite firstFrame = animationFrames[0];

        if (firstFrame != null)
        {
            // Get real sprite size in Unity units
            Vector2 spriteSize = firstFrame.bounds.size;

            DebugHelper.Log($"Sprite: size={spriteSize}, desired hitbox size={size}");

            // For horizontal sprites: swap X and Y!
            float scaleX = size.y / spriteSize.x;  // Hitbox length to sprite length
            float scaleY = size.x / spriteSize.y;  // Hitbox width to sprite width

            targetScale = new Vector3(scaleX, scaleY, 1f);

            DebugHelper.Log($"Rectangle scale: X={scaleX:F2}, Y={scaleY:F2}");
        }
        else
        {
            // Fallback
            targetScale = new Vector3(size.y, size.x, 1f); // Also swap
        }

        // Set position
        transform.position = position;

        // Rotate considering base direction
        if (direction.magnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            float baseAngle = 0f;
            switch (baseDirection)
            {
                case SpriteDirection.North:
                    baseAngle = 90f;
                    break;
                case SpriteDirection.South:
                    baseAngle = -90f;
                    break;
                case SpriteDirection.East:
                    baseAngle = 0f;
                    break;
                case SpriteDirection.West:
                    baseAngle = 180f;
                    break;
            }

            float finalAngle = targetAngle - baseAngle;
            transform.rotation = Quaternion.Euler(0, 0, finalAngle);

            DebugHelper.Log($"Rectangle rotation: target={targetAngle:F1}°, base={baseAngle:F1}°, final={finalAngle:F1}°");
        }

        // Initial scale (slightly smaller)
        transform.localScale = targetScale * 0.7f;

        // Configure SpriteRenderer
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = frames[0];
            spriteRenderer.color = tintColor;
            spriteRenderer.sortingLayerName = "Enemies";
            spriteRenderer.sortingOrder = 10;
        }

        currentFrame = 0;
        frameTimer = 0f;
        isInitialized = true;

        DebugHelper.Log($"Rectangle effect ready: {frames.Length} frames, final scale={targetScale}");
    }
}