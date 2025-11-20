using UnityEngine;
using System.Collections.Generic;

public class VacuumBagProjectile : MonoBehaviour
{
    [Header("Parameters")]
    private float damage = 10f;
    private float speed = 6f;
    private bool isCrit = false;
    private static VacuumBagProjectile activeInstance = null;
    private string weaponName = "";

    [Header("Flight")]
    [SerializeField] private float flyDistance = 3.5f; // Flight distance

    [Header("Vacuum")]
    [SerializeField] private float vacuumRadius = 3f; // Vacuum radius
    [SerializeField] private float vacuumForce = 3f; // Pull force
    [SerializeField] private float duration = 5f; // Duration
    [SerializeField] private float damageInterval = 0.5f; // Damage interval

    // Add these fields at the beginning of the class
    [Header("Vacuum Effect Settings")]
    [Tooltip("Use prefab instead of sprites")]
    public bool usePrefab = false;

    [Tooltip("Prefab with ready animation")]
    public GameObject vacuumEffectPrefab;

    [Tooltip("Sprite array for vacuum animation")]
    public Sprite[] vacuumSprites;

    [Tooltip("Animation speed (frames per second)")]
    public float animationFPS = 12f;

    [Tooltip("Rotate effect?")]
    public bool rotateEffect = true;

    [Tooltip("Rotation speed")]
    public float rotationSpeed = 60f;


    // State
    private enum State { Flying, Vacuuming }
    private State currentState = State.Flying;

    private Vector2 flyDirection;
    private Vector3 startPosition;
    private float activationTime;
    private float lastDamageTime;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;

    // Visual effect
    private GameObject vacuumEffect;

    // Perks
    private PlayerPerks playerPerks;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
    }

    void Start()
    {
        startPosition = transform.position;

        // Start flying
        if (rb != null)
        {
            rb.linearVelocity = flyDirection * speed;
        }
    }

    void Update()
    {
        if (currentState == State.Flying)
        {
            // Check flight distance
            float distanceTraveled = Vector3.Distance(startPosition, transform.position);

            if (distanceTraveled >= flyDistance)
            {
                Land();
            }
        }
        else if (currentState == State.Vacuuming)
        {
            // Vacuum enemies
            VacuumEnemies();

            // Deal damage
            if (Time.time >= lastDamageTime + damageInterval)
            {
                DamageEnemiesInRange();
                lastDamageTime = Time.time;
            }

            // Check lifetime
            if (Time.time >= activationTime + duration)
            {
                Deactivate();
            }
        }
    }

    public void Initialize(Vector2 direction, float dmg, bool crit, PlayerPerks perks, string weapon = "")
    {
        // Check if there's already an active bag
        if (activeInstance != null && activeInstance != this)
        {
            DebugHelper.Log("Already have active bag - destroying old one");
            Destroy(activeInstance.gameObject);
        }

        activeInstance = this;

        flyDirection = direction.normalized;
        damage = dmg;
        isCrit = crit;
        playerPerks = perks;
        weaponName = weapon; // New!

        DebugHelper.Log($"Garbage bag created: damage={dmg}/sec");
    }

    // Landing
    private void Land()
    {
        currentState = State.Vacuuming;
        activationTime = Time.time;
        lastDamageTime = Time.time;

        DebugHelper.Log("Bag landed! Starting vacuum!");

        // Stop
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true; // Become static
        }

        // Increase collider for vacuuming
        if (circleCollider != null)
        {
            circleCollider.radius = vacuumRadius;
            circleCollider.isTrigger = true;
        }

        // Create visual effect
        CreateVacuumEffect();

        // Activation sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCollect(); // Temporary
        }
    }

    // Vacuum enemies
    private void VacuumEnemies()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, vacuumRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Vector2 directionToVacuum = (transform.position - hit.transform.position).normalized;
                float distance = Vector2.Distance(transform.position, hit.transform.position);

                // Disable AI and use direct movement
                EnemyAI enemyAI = hit.GetComponent<EnemyAI>();
                Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();

                if (enemyRb != null)
                {
                    // Temporarily disable AI
                    if (enemyAI != null)
                    {
                        enemyAI.enabled = false;
                    }

                    // Pull force inversely proportional to distance
                    float pullStrength = 1f - (distance / vacuumRadius);
                    pullStrength = Mathf.Clamp01(pullStrength);

                    // Calculate target position
                    Vector2 targetPosition = Vector2.Lerp(
                        enemyRb.position,
                        (Vector2)transform.position,
                        pullStrength * vacuumForce * Time.deltaTime
                    );

                    // Directly move enemy
                    enemyRb.MovePosition(targetPosition);
                }
            }
        }
    }

    // Restore AI to all enemies on deactivation
    private void RestoreEnemyAI()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, vacuumRadius * 2f); // Slightly larger radius

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyAI enemyAI = hit.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.enabled = true;
                }
            }
        }
    }

    // Deal damage to enemies in range
    private void DamageEnemiesInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, vacuumRadius);
        int hitCount = 0;
        float totalDamage = 0f;

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    float damagePerTick = damage * damageInterval;
                    enemyHealth.TakeDamage(damagePerTick, false);
                    hitCount++;
                    totalDamage += damagePerTick;
                }
            }
        }

        if (hitCount > 0)
        {
            // Updated: Weapon damage tracking
            GameStats stats = GameObject.FindObjectOfType<GameStats>();
            if (stats != null)
            {
                stats.AddDamageDealt(totalDamage);

                // New: Per-weapon tracking
                if (!string.IsNullOrEmpty(weaponName))
                {
                    stats.AddWeaponDamage(weaponName, totalDamage);
                }
            }

            DebugHelper.Log($"Bag vacuumed {hitCount} enemies (damage {totalDamage:F1})");
        }
    }


    // Create vacuum visual effect
    private void CreateVacuumEffect()
    {
        if (usePrefab && vacuumEffectPrefab != null)
        {
            // Option 1: Using ready prefab
            vacuumEffect = Instantiate(vacuumEffectPrefab, transform);
            vacuumEffect.transform.localPosition = Vector3.zero;
            vacuumEffect.transform.localScale = Vector3.one * vacuumRadius;

            // Activate Animator if exists
            Animator animator = vacuumEffect.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
            }

            DebugHelper.Log($"Created vacuum effect from prefab: radius={vacuumRadius}");
        }
        else if (vacuumSprites != null && vacuumSprites.Length > 0)
        {
            // Option 2: Animation from sprite array
            vacuumEffect = new GameObject("VacuumEffect");
            vacuumEffect.transform.SetParent(transform);
            vacuumEffect.transform.localPosition = Vector3.zero;
            vacuumEffect.transform.localScale = Vector3.one * vacuumRadius;

            SpriteRenderer sr = vacuumEffect.AddComponent<SpriteRenderer>();
            sr.sprite = vacuumSprites[0];
            sr.sortingLayerName = "Enemies";
            sr.sortingOrder = 5;

            DebugHelper.Log($"Created vacuum effect from {vacuumSprites.Length} sprites: radius={vacuumRadius}");

            // Start sprite animation
            StartCoroutine(AnimateVacuumSprites());
        }
        else
        {
            Debug.LogError("Neither prefab nor sprites assigned for vacuum effect!");
            return;
        }

        // Rotation animation (if enabled)
        if (rotateEffect)
        {
            StartCoroutine(RotateVacuumEffect());
        }
    }

    // Sprite animation
    private System.Collections.IEnumerator AnimateVacuumSprites()
    {
        if (vacuumEffect == null || vacuumSprites == null || vacuumSprites.Length == 0)
            yield break;

        SpriteRenderer sr = vacuumEffect.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        int currentFrame = 0;
        float frameTime = 1f / animationFPS;
        float timer = 0f;

        while (vacuumEffect != null && currentState == State.Vacuuming)
        {
            timer += Time.deltaTime;

            if (timer >= frameTime)
            {
                timer -= frameTime;
                currentFrame = (currentFrame + 1) % vacuumSprites.Length;
                sr.sprite = vacuumSprites[currentFrame];
            }

            yield return null;
        }
    }

    // Effect rotation animation
    private System.Collections.IEnumerator RotateVacuumEffect()
    {
        if (vacuumEffect == null) yield break;

        while (vacuumEffect != null && currentState == State.Vacuuming)
        {
            vacuumEffect.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // Deactivation
    private void Deactivate()
    {
        DebugHelper.Log("Bag disappeared!");

        // Restore enemy AI
        RestoreEnemyAI();

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Just in case restore AI on any destruction
        if (currentState == State.Vacuuming)
        {
            RestoreEnemyAI();
        }

        // New: Clear static reference
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    // Hit wall - land immediately
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (currentState == State.Flying)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
            {
                Land();
            }
        }
    }

    // Editor visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.3f);

        if (currentState == State.Flying)
        {
            Gizmos.DrawWireSphere(transform.position, flyDistance);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, vacuumRadius);
        }
    }
}