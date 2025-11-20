using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public abstract class InteractableObject : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] protected Sprite objectSprite;
    [SerializeField] protected Color objectColor = Color.white;
    [SerializeField] protected Vector2 spriteSize = new Vector2(0.8f, 0.8f);

    [Header("Animation")]
    [SerializeField] protected float bobSpeed = 2f;
    [SerializeField] protected float bobHeight = 0.2f;
    [SerializeField] protected float rotationSpeed = 50f; // Rotation

    [Header("Lifetime")]
    [SerializeField] protected float lifetime = 30f; // Disappears after 30 sec

    protected SpriteRenderer spriteRenderer;
    protected Collider2D objectCollider;
    protected Vector3 startPosition;
    protected float spawnTime;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        objectCollider = GetComponent<Collider2D>();

        // Setup collider as trigger
        objectCollider.isTrigger = true;

        startPosition = transform.position;
        spawnTime = Time.time;
    }

    protected virtual void Start()
    {
        // Apply visual
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = objectSprite;
            spriteRenderer.color = objectColor;
            spriteRenderer.sortingLayerName = "Enemies";
            spriteRenderer.sortingOrder = 5;
        }

        // Scale
        transform.localScale = new Vector3(spriteSize.x, spriteSize.y, 1f);
    }

    protected virtual void Update()
    {
        // Bobbing animation
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Rotation
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

        // Check lifetime
        if (Time.time - spawnTime >= lifetime)
        {
            OnExpire();
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Call pickup effect (implemented in derived classes)
            OnPickup(collision.gameObject);

            // Pickup sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCollect();
            }

            // Destroy object
            Destroy(gameObject);
        }
    }

    // Abstract method - must be implemented in derived classes
    protected abstract void OnPickup(GameObject player);

    // What happens when lifetime expires
    protected virtual void OnExpire()
    {
        DebugHelper.Log($"{GetType().Name} disappeared (lifetime expired)");
        Destroy(gameObject);
    }
}