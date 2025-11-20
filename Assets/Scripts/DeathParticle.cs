using UnityEngine;

public class DeathParticle : MonoBehaviour
{
    private Vector2 velocity;
    private SpriteRenderer spriteRenderer;
    private float lifetime = 0.5f;
    private float elapsed = 0f;
    private float rotationSpeed;
    private float gravity = 5f;

    public void Initialize(Vector2 vel, Color color, Sprite sprite)
    {
        velocity = vel;
        rotationSpeed = Random.Range(-360f, 360f);

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingLayerName = "Enemies";
        spriteRenderer.sortingOrder = 10;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;

        // Apply gravity
        velocity.y -= gravity * Time.deltaTime;

        // Movement
        transform.position += (Vector3)velocity * Time.deltaTime;

        // Rotation
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // Horizontal velocity slowdown
        velocity.x *= 0.95f;

        // Shrink and disappear
        transform.localScale = Vector3.one * (1f - t) * Random.Range(0.8f, 1.2f);

        // Transparency
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f - t;
            spriteRenderer.color = color;
        }


        // Destroy when time runs out
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}