using UnityEngine;

public class WaterSplashParticle : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Vector2 velocity;
    private float lifetime = 1.2f;
    private float maxLifetime = 1.2f;
    private float gravity = -8f;
    private float drag = 0.92f;
    private float rotationSpeed;

    private WaterParticlePool pool;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetPool(WaterParticlePool poolRef)
    {
        pool = poolRef;
    }

    public void Initialize(Vector2 vel, Color color, float size)
    {
        velocity = vel;
        lifetime = maxLifetime;
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }

        transform.localScale = Vector3.one * size;
        rotationSpeed = Random.Range(-360f, 360f);
        
        CancelInvoke();
        Invoke(nameof(ReturnToPool), maxLifetime);
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        
        velocity.y += gravity * Time.deltaTime;
        velocity *= drag;
        transform.position += (Vector3)velocity * Time.deltaTime;
        
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // Fade out
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(1f, 0f, 1f - (lifetime / maxLifetime));
            spriteRenderer.color = color;
        }

        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        CancelInvoke();

        if (pool != null)
        {
            pool.ReturnParticle(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}