using UnityEngine;

public class MeleeAttackEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 0.3f;

    private SpriteRenderer spriteRenderer;
    private float elapsed = 0f;
    private Vector3 targetScale;
    private Color startColor;
    private bool isInitialized = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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

        if (spriteRenderer != null)
        {
            float t = elapsed / duration;
            float currentScale = Mathf.Lerp(0.3f, 1f, Mathf.Min(t * 2f, 1f));
            Vector3 newScale = targetScale * currentScale;
            transform.localScale = newScale;

            // DEBUG only in the first frame
            if (elapsed < Time.deltaTime * 2)
            {
                DebugHelper.Log($"[{gameObject.name}] Frame 1: targetScale={targetScale}, currentScale={currentScale}, newScale={newScale}, actual={transform.localScale}");
            }

            Color color = spriteRenderer.color;
            color.a = startColor.a * (1f - t);
            spriteRenderer.color = color;
        }
    }

    public void Initialize(Vector3 position, Vector3 direction, Color color, float size)
    {
        // Set position
        transform.position = position;

        // Rotate toward attack direction
        if (direction.magnitude > 0.01f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // IMPORTANT: Set the target size
        // size already contains the final effect size
        targetScale = Vector3.one * size;

        // Start small
        transform.localScale = targetScale * 0.3f;

        DebugHelper.Log($"MeleeAttackEffect.Initialize: size={size}, targetScale={targetScale}, startScale={transform.localScale}");

        // Set color
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