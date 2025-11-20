using UnityEngine;

public class Loot : MonoBehaviour
{
    [SerializeField] private int size = 1;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.1f;
    [SerializeField] private float magnetSpeed = 25f;

    private Vector3 startPosition;
    private float bobOffset;
    private MeshRenderer meshRenderer;
    private static MaterialPropertyBlock propertyBlock;

    private bool isCollected = false;
    private bool isBeingPulled = false;

    void OnEnable()
    {
        startPosition = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        isCollected = false;
        isBeingPulled = false;

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (LootManager.Instance != null)
        {
            LootManager.Instance.RegisterLoot(this);
        }
    }

    void OnDisable()
    {
        if (LootManager.Instance != null)
        {
            LootManager.Instance.UnregisterLoot(this);
        }
    }

    // Fixed: Bobbing only when NOT being pulled
    public void UpdateBobbing(float time, float deltaTime)
    {
        if (isBeingPulled) return; // Don’t bob while being pulled

        float newY = startPosition.y + Mathf.Sin((time * bobSpeed) + bobOffset) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        startPosition.x = transform.position.x;
    }

    // Fixed: Set pulling flag
    public void PullToPlayer(Vector3 playerPosition, float deltaTime)
    {
        isBeingPulled = true; // New

        transform.position = Vector3.MoveTowards(
            transform.position,
            playerPosition,
            magnetSpeed * deltaTime
        );

        // New: Update startPosition so after pulling, loot bobs from new position
        startPosition = transform.position;
    }

    // New: Reset pulling flag (called from LootManager)
    public void StopPulling()
    {
        isBeingPulled = false;
    }

    public int GetSize() => size;

    public void SetSize(int newSize)
    {
        size = newSize;

        // Scale based on size:
        // 1-5: small (0.8-1.2)
        // 6-10: medium (1.3-1.7)
        // 11-20: large (1.8-2.5)
        // 21+: huge (2.6-4.0)

        float scale;

        if (newSize <= 5)
        {
            scale = 1.5f + (newSize * 0.08f); // 0.88 - 1.2
        }
        else if (newSize <= 10)
        {
            scale = 2f + ((newSize - 5) * 0.08f); // 1.3 - 1.7
        }
        else if (newSize <= 20)
        {
            scale = 3f + ((newSize - 10) * 0.07f); // 1.8 - 2.5
        }
        else
        {
            scale = 4f + ((newSize - 20) * 0.047f); // 2.6+
            scale = Mathf.Min(scale, 6f); // Max 4x
        }

        transform.localScale = Vector3.one * scale;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        HandleTrigger(collision);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        HandleTrigger(collision);
    }

    private void HandleTrigger(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            GarbageBag bag = collision.GetComponent<GarbageBag>();

            if (bag != null && bag.TryAddGarbage(size))
            {
                CollectLoot();
            }
        }
        else if (collision.CompareTag("Water"))
        {
            DestroyLoot();
        }
    }

    private void CollectLoot()
    {
        isCollected = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCollect();
        }

        ReturnToPool();
    }

    private void DestroyLoot()
    {
        isCollected = true;
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (LootPool.Instance != null)
        {
            LootPool.Instance.ReturnLoot(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateStartPosition(Vector3 newPosition)
    {
        startPosition = newPosition;
    }


}