using UnityEngine;
using System.Collections.Generic;

public class ExplodingProjectile : Projectile
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionDamage = 10f;
    [SerializeField] private Color explosionColor = new Color(1f, 0.5f, 0f);

    private string weaponName = ""; // New!

    // Updated: Added weapon parameter
    public void Initialize(Vector2 dir, float dmg, bool crit, Color color, float projectileSpeed, PlayerPerks perks, string weapon = "", float explosionDmg = 0)
    {
        // Call base Initialize (it now also accepts weapon)
        base.Initialize(dir, dmg, crit, color, projectileSpeed, perks, weapon);

        // Save weapon name
        weaponName = weapon;

        // Set explosion damage
        if (explosionDmg > 0)
        {
            explosionDamage = explosionDmg;
        }
    }

    protected override void OnEnemyHit(Collider2D enemyCollider, EnemyHealth enemyHealth)
    {
        // First deal normal damage to target (base class already tracks this)
        base.OnEnemyHit(enemyCollider, enemyHealth);

        // Then create explosion
        Explode(enemyCollider.transform.position);

        // Destroy projectile
        Destroy(gameObject);
    }

    private void Explode(Vector3 explosionCenter)
    {
        DebugHelper.Log($"EXPLOSION at {explosionCenter}!");

        CreateExplosionEffect(explosionCenter);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySwordAttack();
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(explosionCenter, explosionRadius);
        int hitCount = 0;
        float totalExplosionDamage = 0f;

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                // Skip first target (it already received damage)
                if (hitEnemies.Contains(hit.gameObject))
                    continue;

                EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(explosionDamage, false);
                    hitCount++;
                    totalExplosionDamage += explosionDamage;

                    DebugHelper.Log($"Explosion hit {hit.name} ({explosionDamage} damage)");
                }
            }
        }

        DebugHelper.Log($"Explosion hit {hitCount} enemies");

        // Updated: Explosion damage tracking
        if (hitCount > 0)
        {
            GameStats stats = GameObject.FindObjectOfType<GameStats>();
            if (stats != null)
            {
                stats.AddDamageDealt(totalExplosionDamage);

                // New: Per-weapon tracking (explosion damage)
                if (!string.IsNullOrEmpty(weaponName))
                {
                    stats.AddWeaponDamage(weaponName + " (Explosion)", totalExplosionDamage);
                }
            }
        }
    }

    private void CreateExplosionEffect(Vector3 position)
    {
        GameObject explosion = new GameObject("Explosion");
        explosion.transform.position = position;

        SpriteRenderer sr = explosion.AddComponent<SpriteRenderer>();

        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDist = distance / (size / 2f);

                float alpha = 1f - normalizedDist;
                alpha = Mathf.Clamp01(alpha);

                Color pixelColor;
                if (normalizedDist < 0.3f)
                {
                    pixelColor = new Color(1f, 1f, 0.8f, alpha);
                }
                else if (normalizedDist < 0.6f)
                {
                    pixelColor = new Color(1f, 0.6f, 0.2f, alpha);
                }
                else
                {
                    pixelColor = new Color(0.8f, 0.3f, 0.1f, alpha * 0.5f);
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }
        texture.Apply();

        sr.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 2f);
        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 20;

        Destroy(explosion, 0.5f);

        MonoBehaviour mono = FindObjectOfType<MonoBehaviour>();
        if (mono != null)
        {
            mono.StartCoroutine(ExplosionAnimation(explosion, explosionRadius));
        }
    }

    private System.Collections.IEnumerator ExplosionAnimation(GameObject explosion, float maxRadius)
    {
        SpriteRenderer sr = explosion.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        float duration = 0.4f;

        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * (maxRadius * 2f);

        while (elapsed < duration && explosion != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            explosion.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            Color color = sr.color;
            color.a = 1f - t;
            sr.color = color;

            yield return null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}