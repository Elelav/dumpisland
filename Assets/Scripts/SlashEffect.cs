using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SlashEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Vector2 baseSize = new Vector2(1f, 3f);

    private SpriteRenderer spriteRenderer;
    private float elapsed = 0f;
    private Vector2 targetSize;
    private Color startColor;
    private bool isInitialized = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Fixed: Use cache!
        spriteRenderer.sprite = SpriteCache.Instance.GetSlashSprite(32, 96);

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        elapsed += Time.deltaTime;

        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }

        float t = elapsed / duration;

        // Expand
        float currentScale = Mathf.Lerp(0.3f, 1f, Mathf.Min(t * 2f, 1f));
        transform.localScale = new Vector3(targetSize.x * currentScale, targetSize.y * currentScale, 1f);

        // Fade out
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = startColor.a * (1f - t);
            spriteRenderer.color = color;
        }
    }

    public void Initialize(Vector3 position, Vector3 direction, Color color, float size)
    {
        float forwardOffset = 0.5f;
        Vector3 finalPosition = position + (direction * forwardOffset);

        transform.position = finalPosition;

        if (direction.magnitude > 0.01f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        float width = size * baseSize.x;
        float height = size * baseSize.y;

        targetSize = new Vector2(width, height);
        transform.localScale = new Vector3(targetSize.x * 0.3f, targetSize.y * 0.3f, 1f);

        DebugHelper.Log($"SlashEffect: size={size}, baseSize={baseSize}, targetSize={targetSize}");

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            startColor = color;
            spriteRenderer.color = color;
        }

        elapsed = 0f;
        isInitialized = true;
    }
}