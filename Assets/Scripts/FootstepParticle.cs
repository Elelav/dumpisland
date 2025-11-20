using UnityEngine;

public class FootstepParticle : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float lifetime = 0.8f;
    private float fadeSpeed = 1f;
    private Vector3 targetScale;
    private Vector3 startScale;

    public void Initialize(Color color, float size)
    {
        // SpriteRenderer
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        // Try to load sprite
        spriteRenderer.sprite = Resources.Load<Sprite>("UI/Skin/Knob");

        // Fixed: use cache instead of creating a new texture
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = SpriteCache.Instance.GetCircleSprite(32);
        }

        spriteRenderer.color = color;
        spriteRenderer.sortingLayerName = "Ground";
        spriteRenderer.sortingOrder = 10;

        startScale = Vector3.one * (size * 0.5f);
        targetScale = Vector3.one * size;
        transform.localScale = startScale;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Smoothly increase in size
        if (transform.localScale.x < targetScale.x)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * 8f
            );
        }

        // Gradual fade out
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a -= fadeSpeed * Time.deltaTime;
            spriteRenderer.color = color;

            if (color.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}