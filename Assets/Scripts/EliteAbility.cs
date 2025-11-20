using UnityEngine;

public class EliteAbility : MonoBehaviour
{
    [Header("Ability")]
    [SerializeField] private float abilityCooldown = 5f;
    [SerializeField] private float abilityRange = 4f;
    [SerializeField] private float abilityDamage = 15f;

    [Header("Visual")]
    [SerializeField] private Color abilityColor = Color.red;

    private float lastAbilityTime = 0f;
    private Transform player;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        if (Time.time >= lastAbilityTime + abilityCooldown)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= abilityRange * 1.5f)
            {
                UseAbility();
                lastAbilityTime = Time.time;
            }
        }
    }

    private void UseAbility()
    {
        DebugHelper.Log("Elite enemy uses ability!");

        // Visual effect
        StartCoroutine(AbilityVisualEffect());

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEliteAbility();
        }

        // Check hit on player
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= abilityRange)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(abilityDamage);
                    DebugHelper.Log($"Elite ability hit! Damage: {abilityDamage}");
                }
            }
        }
    }

    private System.Collections.IEnumerator AbilityVisualEffect()
    {
        GameObject effectObj = new GameObject("EliteAbilityEffect");
        effectObj.transform.position = transform.position;

        SpriteRenderer sr = effectObj.AddComponent<SpriteRenderer>();

        // Fixed: Using SpriteCache!
        if (SpriteCache.Instance != null)
        {
            sr.sprite = SpriteCache.Instance.GetCircleSprite(64); // Circle sprite 64x64
        }
        else
        {
            // Fallback: Try to load from Resources
            sr.sprite = Resources.Load<Sprite>("UI/Skin/Knob");

            // New: Check that sprite loaded
            if (sr.sprite == null)
            {
                Debug.LogWarning("EliteAbility: Failed to load sprite! Creating programmatically.");
                sr.sprite = CreateCircleSprite(64);
            }
        }

        sr.color = abilityColor;
        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 10;

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale = Mathf.Lerp(0.5f, abilityRange * 2, t);
            effectObj.transform.localScale = Vector3.one * scale;

            Color color = sr.color;
            color.a = (1f - t) * 0.6f; // Slightly more transparent
            sr.color = color;

            yield return null;
        }

        Destroy(effectObj);
    }

    // New: Create circle sprite programmatically (fallback)
    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = distance < radius ? 1f - (distance / radius) : 0f;
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, abilityRange);
    }
}