using UnityEngine;

public class FlameProjectile : MonoBehaviour
{
    private float damage = 1f;
    private float speed = 8f;
    private float lifetime = 0.6f;
    private string weaponName = ""; // New!

    private Vector2 direction;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        StartCoroutine(FadeOut());
    }

    void Update()
    {
        transform.Rotate(0f, 0f, 360f * Time.deltaTime);
    }

    // Updated: Added weapon parameter
    public void Initialize(Vector2 dir, float dmg, float projectileSpeed, string weapon = "")
    {
        direction = dir.normalized;
        damage = dmg;
        speed = projectileSpeed;
        weaponName = weapon; // New!
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage, false);

                // Updated: Weapon damage tracking
                GameStats stats = GameObject.FindObjectOfType<GameStats>();
                if (stats != null)
                {
                    stats.AddDamageDealt(damage);

                    // New: Per-weapon tracking
                    if (!string.IsNullOrEmpty(weaponName))
                    {
                        stats.AddWeaponDamage(weaponName, damage);
                    }
                }
            }

            // Don't destroy - full pierce!
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator FadeOut()
    {
        if (spriteRenderer == null) yield break;

        Color startColor = spriteRenderer.color;
        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / lifetime);

            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }
    }
}