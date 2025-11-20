using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Attack")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    // Knockback
    private bool isKnockedBack = false;
    private float knockbackEndTime = 0f;

    private Transform player;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            DebugHelper.LogError("Player not found! Make sure Player has the 'Player' tag");
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // Skip movement if knocked back
        if (isKnockedBack)
        {
            // Check if knockback ended
            if (Time.time >= knockbackEndTime)
            {
                isKnockedBack = false;
                rb.linearVelocity = Vector2.zero; // Stop
            }
            return; // Don’t move toward player
        }

        // Normal movement toward player
        Vector2 direction = (player.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                    lastAttackTime = Time.time;
                }
            }
        }
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    // Knockback
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (rb == null) return;

        // Set knockback flag
        isKnockedBack = true;
        knockbackEndTime = Time.time + duration;

        // Apply knockback
        rb.linearVelocity = direction * force;

        DebugHelper.Log($"{gameObject.name} knocked back for {duration}s");
    }

    // Apply wave modifiers
    public void ApplyWaveModifiers(float damageMultiplier, float speedMultiplier)
    {
        // Increase base damage
        damage *= damageMultiplier;

        // Increase speed
        moveSpeed *= speedMultiplier;

        DebugHelper.Log($"EnemyAI modified: damage={damage:F1}, speed={moveSpeed:F1}");
    }
}