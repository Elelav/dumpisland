using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] public float moveSpeed = 5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    public Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down; // Last direction (for Idle)

    private float baseMoveSpeed;
    private float moveSpeedMultiplierFromPerks = 1f;

    void Awake()
    {
        baseMoveSpeed = moveSpeed;
        rb = GetComponent<Rigidbody2D>();

        // Get Animator (if not assigned)
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    void Update()
    {
        // Movement input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();

        // Update animation
        UpdateAnimation();

        //// Testing (keep as is)
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    GetComponent<PlayerHealth>().TakeDamage(10);
        //}

        //if (Input.GetMouseButtonDown(0))
        //{
        //    AttackNearestEnemy();
        //}

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            WeaponData harpoonPike = Resources.Load<WeaponData>("Items/Weapons/OmniWeapon_Cheat");
            if (harpoonPike != null)
            {
                GetComponent<PlayerInventory>().AddWeapon(harpoonPike);
            }
        }
    }

    void FixedUpdate()
    {
        // Move player
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    // Update animation
    private void UpdateAnimation()
    {
        if (animator == null) return;

        bool isMoving = moveInput.magnitude > 0.01f;

        // Update Animator parameters
        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            // Remember direction for Idle
            lastMoveDirection = moveInput;

            // Pass direction to BlendTree
            animator.SetFloat("MoveX", moveInput.x);
            animator.SetFloat("MoveY", moveInput.y);
        }
        else
        {
            // Idle - use last direction
            animator.SetFloat("MoveX", lastMoveDirection.x);
            animator.SetFloat("MoveY", lastMoveDirection.y);
        }
    }

    // Temporary attack method
    private void AttackNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0) return;

        GameObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        if (nearest != null && minDistance <= 5f)
        {
            EnemyHealth enemyHealth = nearest.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(15);
                DebugHelper.Log("Attacked an enemy!");
            }
        }
        else
        {
            DebugHelper.Log("Enemies are too far away!");
        }
    }

    public void UpdateMoveSpeed(float multiplier)
    {
        moveSpeedMultiplierFromPerks = multiplier;
        moveSpeed = baseMoveSpeed * multiplier;

        DebugHelper.Log($"Speed updated: {moveSpeed} (base: {baseMoveSpeed}, multiplier: x{multiplier})");
    }

    public float GetCurrentMoveSpeed()
    {
        return moveSpeed;
    }
}