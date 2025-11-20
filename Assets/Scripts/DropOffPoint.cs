using UnityEngine;
using System.Collections;

public class DropOffPoint : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private Color inactiveColor = Color.gray;

    private SpriteRenderer spriteRenderer;
    private bool playerInRange = false;
    private bool isActive = false;
    private GarbageBag playerBag;
    private PlayerMoney playerMoney;

    [Header("Automatic Drop-off")]
    [SerializeField] private float dropOffDuration = 1.5f;
    [SerializeField] private DropOffProgressUI progressUI;

    [Header("Drop-off Animation")]
    [SerializeField] private DropOffAnimationEffect animationEffect;
    [SerializeField] private Sprite[] dropOffAnimationFrames;
    [SerializeField] private float animationFPS = 12f;

    [Header("Sounds")]
    [SerializeField] private AudioClip dropOffProcessSound;
    [SerializeField] private AudioClip dropOffCompleteSound;

    private Coroutine dropOffCoroutine;
    private bool isDropping = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            DebugHelper.LogError($"{gameObject.name} has no SpriteRenderer!");
        }

        // Create animation component if it doesn't exist
        if (animationEffect == null)
        {
            GameObject animObj = new GameObject("DropOffAnimation");
            animObj.transform.SetParent(transform);
            animObj.transform.localPosition = Vector3.zero;
            animObj.transform.localScale = Vector3.one;

            animationEffect = animObj.AddComponent<DropOffAnimationEffect>();

            SpriteRenderer animSR = animObj.AddComponent<SpriteRenderer>();
            animSR.sortingLayerName = "Effects";
            animSR.sortingOrder = 10;
        }
    }

    void Update()
    {
        if (!isActive)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = inactiveColor;
            }
            return;
        }

        // Visual: pulsing of active point
        if (spriteRenderer != null && !isDropping)
        {
            float pulse = Mathf.Sin(Time.time * 3f) * 0.1f + 0.9f;
            spriteRenderer.color = playerInRange ? activeColor * pulse : normalColor * pulse;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && isActive)
        {
            playerInRange = true;
            playerBag = collision.GetComponent<GarbageBag>();
            playerMoney = collision.GetComponent<PlayerMoney>();

            // Check if there's garbage in bag
            if (playerBag != null && playerBag.GetCurrentCapacity() > 0)
            {
                StartDropOff();
            }
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // If player in zone but process not started (e.g., bag was empty)
        if (collision.CompareTag("Player") && isActive && !isDropping)
        {
            if (playerBag != null && playerBag.GetCurrentCapacity() > 0)
            {
                StartDropOff();
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;

            // Cancel drop-off process
            CancelDropOff();

            playerBag = null;
            playerMoney = null;
        }
    }

    private void StartDropOff()
    {
        DebugHelper.Log($"[DropOff] ===== StartDropOff CALLED =====");
        DebugHelper.Log($"[DropOff] isDropping: {isDropping}, playerBag: {(playerBag != null ? "OK" : "NULL")}, garbage: {(playerBag != null ? playerBag.GetCurrentCapacity() : 0)}");

        if (isDropping || playerBag == null || playerBag.GetCurrentCapacity() == 0)
        {
            DebugHelper.LogWarning($"[DropOff] Canceling start: isDropping={isDropping}, bag={playerBag != null}, garbage={(playerBag != null ? playerBag.GetCurrentCapacity() : 0)}");
            return;
        }

        isDropping = true;

        // Show progress bar
        if (progressUI != null)
        {
            DebugHelper.Log("[DropOff] Calling progressUI.Show()");
            progressUI.Show();
        }
        else
        {
            DebugHelper.LogError("[DropOff] progressUI = NULL! Assign ProgressCanvas in Inspector!");
        }

        // Start animation
        if (animationEffect != null && dropOffAnimationFrames != null && dropOffAnimationFrames.Length > 0)
        {
            DebugHelper.Log($"[DropOff] Starting animation ({dropOffAnimationFrames.Length} frames)");
            animationEffect.PlayAnimation(dropOffAnimationFrames, animationFPS);
        }

        // Start process sound
        if (dropOffProcessSound != null)
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                AudioManager.Instance.PlaySound(dropOffProcessSound);
            }
        }

        // Start coroutine
        DebugHelper.Log("[DropOff] Starting coroutine AutoDropOffCoroutine");
        dropOffCoroutine = StartCoroutine(AutoDropOffCoroutine());
    }

    private void CancelDropOff()
    {
        if (!isDropping)
            return;

        isDropping = false;

        // Stop coroutine
        if (dropOffCoroutine != null)
        {
            StopCoroutine(dropOffCoroutine);
            dropOffCoroutine = null;
        }

        // Hide UI
        if (progressUI != null)
        {
            progressUI.Hide();
        }

        // Stop animation
        if (animationEffect != null)
        {
            animationEffect.StopAnimation();
        }
    }

    private IEnumerator AutoDropOffCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < dropOffDuration)
        {
            // If player left the zone
            if (!playerInRange)
            {
                CancelDropOff();
                yield break;
            }

            elapsed += Time.deltaTime;
            float progress = elapsed / dropOffDuration;

            // Update progress bar
            if (progressUI != null)
            {
                progressUI.SetProgress(progress);
            }

            yield return null;
        }

        // Process completed - drop off garbage
        CompleteDropOff();
    }

    private void CompleteDropOff()
    {
        if (playerBag == null || playerMoney == null)
        {
            CancelDropOff();
            return;
        }

        // Empty bag and get money
        int moneyEarned = playerBag.EmptyBag();

        if (moneyEarned > 0)
        {
            playerMoney.AddMoney(moneyEarned);

            // Completion sound (use ready method if sound not set)
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                if (dropOffCompleteSound != null)
                {
                    AudioManager.Instance.PlaySound(dropOffCompleteSound);
                }
                else
                {
                    AudioManager.Instance.PlayDropOff(); // Use default sound
                }
            }

            // Visual effect
            StartCoroutine(DropOffEffect());

            // Optional: change active point
            DropOffManager manager = FindObjectOfType<DropOffManager>();
            if (manager != null)
            {
                manager.OnGarbageDroppedOff();
            }
        }

        // Hide UI and animation
        if (progressUI != null)
        {
            progressUI.Hide();
        }

        if (animationEffect != null)
        {
            animationEffect.StopAnimation();
        }

        isDropping = false;
        dropOffCoroutine = null;
    }

    private IEnumerator DropOffEffect()
    {
        if (spriteRenderer == null) yield break;

        // Flash several times
        for (int i = 0; i < 5; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = activeColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    // Public methods for control
    public void SetActive(bool active)
    {
        isActive = active;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = active ? normalColor : inactiveColor;
        }

        // Cancel drop-off if point is deactivated
        if (!active && isDropping)
        {
            CancelDropOff();
        }

        DebugHelper.Log($"{gameObject.name} - SetActive({active})");
    }

    public bool IsActive() => isActive;
    public bool IsDropping() => isDropping;
}