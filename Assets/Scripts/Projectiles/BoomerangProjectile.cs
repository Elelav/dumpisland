using UnityEngine;
using System.Collections.Generic;

public class BoomerangProjectile : MonoBehaviour
{
    [Header("Parameters")]
    private float damage = 10f;
    private float speed = 8f;
    private bool isCrit = false;
    private string weaponName = ""; // New!

    [Header("Boomerang")]
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private float returnSpeedMultiplier = 1.2f;

    // Pierce
    private int remainingPierceCount = 0;
    private List<GameObject> hitEnemies = new List<GameObject>();
    private List<GameObject> chainedEnemies = new List<GameObject>(); // Hit by lightning
    private PlayerPerks playerPerks;

    // State
    private enum State { GoingOut, Returning }
    private State currentState = State.GoingOut;

    private Transform player;
    private Vector2 initialDirection;
    private Vector3 startPosition;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        startPosition = transform.position;

        if (rb != null)
        {
            rb.linearVelocity = initialDirection * speed;
        }
    }

    void Update()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.Rotate(0f, 0f, 720f * Time.deltaTime);

        if (currentState == State.GoingOut)
        {
            float distanceTraveled = Vector3.Distance(startPosition, transform.position);
            if (distanceTraveled >= maxDistance)
            {
                StartReturning();
            }
        }
        else if (currentState == State.Returning)
        {
            Vector2 directionToPlayer = (player.position - transform.position).normalized;

            if (rb != null)
            {
                rb.linearVelocity = directionToPlayer * speed * returnSpeedMultiplier;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer < 0.5f)
            {
                DebugHelper.Log("Boomerang returned!");
                Destroy(gameObject);
            }
        }
    }

    // Updated: Added weapon parameter
    public void Initialize(Vector2 direction, float dmg, bool crit, Color color, float projectileSpeed, PlayerPerks perks, string weapon = "")
    {
        initialDirection = direction.normalized;
        damage = dmg;
        isCrit = crit;
        speed = projectileSpeed;
        playerPerks = perks;
        weaponName = weapon; // New!

        if (playerPerks != null)
        {
            remainingPierceCount = playerPerks.GetMaxPierceCount();
        }

        if (isCrit && spriteRenderer != null)
        {
            CreateCritGlow();
        }

        DebugHelper.Log($"Boomerang created: damage={dmg}, pierces={remainingPierceCount}");
    }

    private void StartReturning()
    {
        currentState = State.Returning;
        hitEnemies.Clear();
        DebugHelper.Log("Boomerang turning back!");
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            if (hitEnemies.Contains(collision.gameObject))
            {
                return;
            }

            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage, isCrit);
                hitEnemies.Add(collision.gameObject);
                CreateHitEffect(collision.transform.position);
                // Chain lightning
                if (playerPerks != null && playerPerks.HasPerk("chainLightning"))
                {
                    TryChainLightning(collision.transform.position);
                }

                // Updated: Weapon damage tracking
                GameStats stats = GameObject.FindObjectOfType<GameStats>();
                if (stats != null)
                {
                    stats.AddDamageDealt(damage);

                    // New: Per-weapon tracking
                    if (!string.IsNullOrEmpty(weaponName))
                    {
                        string directionText = currentState == State.GoingOut ? "" : " (Return)";
                        stats.AddWeaponDamage(weaponName + directionText, damage);
                    }
                }

                string directionText2 = currentState == State.GoingOut ? "forward" : "backwards";
                DebugHelper.Log($"Boomerang ({directionText2}) hit {collision.name}, pierces: {remainingPierceCount}");

                if (remainingPierceCount > 0)
                {
                    remainingPierceCount--;
                    return;
                }

                Destroy(gameObject);
            }
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            if (currentState == State.GoingOut)
            {
                StartReturning();
            }
        }

        if (collision.CompareTag("Player") && currentState == State.Returning)
        {
            DebugHelper.Log("Boomerang caught by player!");
            Destroy(gameObject);
        }
    }

    private void CreateCritGlow()
    {
        GameObject glow = new GameObject("CritGlow");
        glow.transform.SetParent(transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localRotation = Quaternion.identity;
        glow.transform.localScale = Vector3.one * 1.3f;

        SpriteRenderer glowRenderer = glow.AddComponent<SpriteRenderer>();

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            glowRenderer.sprite = spriteRenderer.sprite;
        }

        glowRenderer.color = new Color(1f, 1f, 0f, 0.5f);
        glowRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        glowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

        StartCoroutine(PulseGlow(glow));
    }

    private System.Collections.IEnumerator PulseGlow(GameObject glow)
    {
        float elapsed = 0f;
        Vector3 baseScale = Vector3.one * 1.3f;

        while (glow != null)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.Sin(elapsed * 10f) * 0.2f + 1f;
            glow.transform.localScale = baseScale * pulse;
            yield return null;
        }
    }

    private void CreateHitEffect(Vector3 position)
    {
        GameObject hitEffect = new GameObject("BoomerangHit");
        hitEffect.transform.position = position;

        SpriteRenderer sr = hitEffect.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("UI/Skin/Knob");

        if (spriteRenderer != null)
        {
            sr.color = isCrit ? Color.yellow : spriteRenderer.color;
        }
        else
        {
            sr.color = isCrit ? Color.yellow : Color.cyan;
        }

        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 15;

        Destroy(hitEffect, 0.2f);

        StartCoroutine(HitEffectAnimation(hitEffect));
    }

    private System.Collections.IEnumerator HitEffectAnimation(GameObject effect)
    {
        SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration && effect != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            effect.transform.localScale = Vector3.one * (1f - t) * 1.2f;

            Color color = sr.color;
            color.a = 1f - t;
            sr.color = color;

            yield return null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (currentState == State.GoingOut)
        {
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
        else
        {
            if (player != null)
            {
                Gizmos.DrawLine(transform.position, player.position);
            }
        }
    }

    private void TryChainLightning(Vector3 fromPosition)
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(fromPosition, 5f);

        int chainCount = 0;
        int maxChains = 3;

        foreach (Collider2D enemy in nearbyEnemies)
        {
            if (!enemy.CompareTag("Enemy")) continue;

            // Check BOTH lists
            // Don't hit those who already received direct hit OR already hit by lightning
            if (hitEnemies.Contains(enemy.gameObject) || chainedEnemies.Contains(enemy.gameObject))
            {
                continue;
            }

            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                float chainDamage = damage * 0.1f;
                enemyHealth.TakeDamage(chainDamage, false);

                // Chain lightning damage
                GameStats stats = GameObject.FindObjectOfType<GameStats>();
                if (stats != null && !string.IsNullOrEmpty(weaponName))
                {
                    stats.AddWeaponDamage(weaponName + " (Chain lightning)", chainDamage);
                }

                CreateLightningEffect(fromPosition, enemy.transform.position);

                // Add to lightning list (NOT to hitEnemies!)
                chainedEnemies.Add(enemy.gameObject);
                chainCount++;

                DebugHelper.Log($"Lightning hit {enemy.name} ({chainDamage:F0} damage)");

                if (chainCount >= maxChains) break;
            }
        }

        if (chainCount > 0)
        {
            DebugHelper.Log($"Chain lightning: {chainCount} bounces!");
        }
    }
    private void CreateLightningEffect(Vector3 from, Vector3 to)
    {
        GameObject lightning = new GameObject("Lightning");
        LineRenderer lr = lightning.AddComponent<LineRenderer>();

        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.cyan;
        lr.endColor = Color.white;
        lr.sortingLayerName = "Enemies";
        lr.sortingOrder = 10;

        Destroy(lightning, 0.1f);
    }


}