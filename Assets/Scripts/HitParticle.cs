using UnityEngine;

public class HitParticle : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Vector2 velocity;
    private float lifetime = 0.5f;
    private float maxLifetime = 0.5f;
    private float gravity = -10f;
    private float drag = 0.95f;

    private HitParticlePool pool;

    public void SetPool(HitParticlePool poolRef)
    {
        pool = poolRef;
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingLayerName = "Effects";
        spriteRenderer.sortingOrder = 10;
    }

    public void Initialize(Vector2 vel, Color color, bool isCrit = false)
    {
        velocity = vel;
        lifetime = isCrit ? 0.7f : 0.5f;
        maxLifetime = lifetime;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        spriteRenderer.color = color;

        if (isCrit)
        {
            spriteRenderer.color = Color.Lerp(Color.yellow, Color.white, 0.5f);
            transform.localScale = Vector3.one * Random.Range(0.15f, 0.25f);
        }
        else
        {
            transform.localScale = Vector3.one * Random.Range(0.08f, 0.15f);
        }

        CancelInvoke();
        Invoke(nameof(ReturnToPool), maxLifetime);
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        velocity.y += gravity * Time.deltaTime;
        velocity *= drag;
        transform.position += (Vector3)velocity * Time.deltaTime;

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