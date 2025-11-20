using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Armor")]
    [SerializeField] private float armor = 0f; // Current armor

    [Header("Invulnerability")]
    [SerializeField] private float invulnerabilityDuration = 0.5f;
    [SerializeField] private float damageSpeedBoost = 1.5f; // Speed multiplier
    private bool isInvulnerable = false;
    private float invulnerabilityTimer = 0f;

    [Header("Regeneration")]
    [SerializeField] private float healthRegenRate = 0f; // HP per second
    [SerializeField] private float regenEffectInterval = 0.5f; // Visual effect interval

    [SerializeField]
    private ActivePerksUI activePerks;

    private float regenEffectTimer = 0f;

    // Events for UI
    public UnityEvent<float, float> OnHealthChanged; // (current, max)
    public UnityEvent<float> OnArmorChanged;
    public UnityEvent OnDeath;

    private PlayerController playerController;
    private SpriteRenderer spriteRenderer;

    private float baseMaxHealth; // Base value (without perks)
    private float healthBonusFromPerks = 0f;
    private float healthMultiplierFromPerks = 1f;

    private bool isHited = false;

    void Start()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Notify UI of initial health
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        // Invulnerability timer
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;

            // Simple blinking effect
            spriteRenderer.enabled = (Time.time % 0.2f) > 0.1f;

            if (invulnerabilityTimer <= 0)
            {
                isInvulnerable = false;
                spriteRenderer.enabled = true;
            }
        }

        // Health regeneration
        if (healthRegenRate > 0 && currentHealth < maxHealth)
        {
            float regenAmount = healthRegenRate * Time.deltaTime;
            currentHealth += regenAmount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Regeneration visual effect
            regenEffectTimer += Time.deltaTime;
            if (regenEffectTimer >= regenEffectInterval)
            {
                regenEffectTimer = 0f;
                ShowRegenEffect();
            }
        }
    }

    void Awake()
    {
        baseMaxHealth = maxHealth;
    }

    public void TakeDamage(float damage, bool isCrit = false)
    {
        if (isInvulnerable) return;

        // Check dodge chance
        if (TryDodge())
        {
            // Visual feedback for dodge
            if (DamageNumberManager.Instance != null)
            {
                DamageNumberManager.Instance.ShowDamage(0, transform.position, false);
            }
            return; // No damage taken!
        }

        // Apply damage taken multiplier from perks
        damage *= GetDamageTakenMultiplier();

        // Reduce damage by armor
        float damageAfterArmor = Mathf.Max(damage - armor, 0);
        float blockedDamage = damage - damageAfterArmor;
        damage = damageAfterArmor;

        if (blockedDamage > 0)
        {
            DebugHelper.Log($"Armor absorbed {blockedDamage:F1} damage");
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        DebugHelper.Log($"Player took {damage} damage. Remaining HP: {currentHealth}");

        // SHOW DAMAGE NUMBER
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.ShowDamage(damage, transform.position, isCrit);
        }

        // Hit sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerHit();
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        isHited = true;

        if (currentHealth <= 0)
        {
            // Try activating Guardian Angel before death
            if (TryActivateGuardianAngel())
            {
                return; // Resurrection occurred, do not die!
            }

            Die();
        }
        else
        {
            isInvulnerable = true;
            invulnerabilityTimer = invulnerabilityDuration;
            StartCoroutine(SpeedBoostCoroutine());
        }
    }

    private System.Collections.IEnumerator SpeedBoostCoroutine()
    {
        float originalSpeed = playerController.moveSpeed;
        playerController.moveSpeed *= damageSpeedBoost;

        yield return new WaitForSeconds(invulnerabilityDuration);

        playerController.moveSpeed = originalSpeed;
    }

    private void Die()
    {
        DebugHelper.Log("Player died!");

        // Death sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerDeath();
        }

        // Disable all player components
        GetComponent<PlayerController>().enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Stop movement
            rb.simulated = false; // Disable physics
        }

        // Disable colliders (so enemies don't attack corpse)
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Make sprite semi-transparent
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0.5f;
            spriteRenderer.color = color;
        }

        // Disable health script (to prevent further damage)
        this.enabled = false;

        OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void UpdateMaxHealth(float bonus, float multiplier)
    {
        healthBonusFromPerks = bonus;
        healthMultiplierFromPerks = multiplier;

        // New max HP = (base + bonus) * multiplier
        float newMaxHealth = (baseMaxHealth + bonus) * multiplier;

        // Preserve health percentage
        float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 1f;

        maxHealth = newMaxHealth;
        currentHealth = maxHealth * healthPercent;

        DebugHelper.Log($"Max HP updated: {maxHealth} (base: {baseMaxHealth}, bonus: {bonus}, multiplier: x{multiplier})");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // For shop
    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount; // Also increase current HP

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        DebugHelper.Log($"Max HP increased by {amount}! New maximum: {maxHealth}");
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;

    public void UpdateArmor(float armorValue)
    {
        armor = armorValue;
        OnArmorChanged?.Invoke(armor);

        DebugHelper.Log($"Armor updated: {armor}");
    }

    public float GetArmor() => armor;

    // Get damage taken multiplier from perks
    public float GetDamageTakenMultiplier()
    {
        PlayerPerks perks = GetComponent<PlayerPerks>();
        if (perks != null)
        {
            return perks.GetTotalStatMultiplier("damageTaken");
        }
        return 1f;
    }

    // Check dodge chance
    public bool TryDodge()
    {
        PlayerPerks perks = GetComponent<PlayerPerks>();
        if (perks != null)
        {
            float dodgeChance = perks.GetTotalBonus("dodgeChance");

            if (dodgeChance > 0 && Random.value < dodgeChance)
            {
                DebugHelper.Log($"DODGE! (chance: {dodgeChance * 100:F0}%)");
                return true;
            }
        }
        return false;
    }

    // Try to activate Guardian Angel
    private bool TryActivateGuardianAngel()
    {
        PlayerPerks perks = GetComponent<PlayerPerks>();
        if (perks == null) return false;

        // Check if perk exists
        if (!perks.HasGuardianAngel()) return false;

        DebugHelper.Log("GUARDIAN ANGEL ACTIVATED! Resurrection!");

        // Restore 100 HP (clamped to max)
        currentHealth = 100f;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Resurrection visual effect
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.ShowDamage(0, transform.position + Vector3.up, false);
            // Add custom revival effect later if desired
        }

        // Resurrection sound (optional)
        if (AudioManager.Instance != null)
        {
            // AudioManager.Instance.PlayRevive(); // Add sound if needed
        }

        // REMOVE perk (one-time use)
        perks.RemoveGuardianAngel();
        activePerks.RefreshAllPerks();

        // Grant extended invulnerability after resurrection
        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityDuration * 2f;

        return true; // Resurrection successful
    }

    public void UpdateHealthRegen(float regenValue)
    {
        healthRegenRate = regenValue;

        DebugHelper.Log($"UpdateHealthRegen called: regenValue={regenValue}, healthRegenRate={healthRegenRate}");

        if (regenValue > 0)
        {
            DebugHelper.Log($"Health regeneration: {regenValue:F1} HP/sec");
        }
    }

    public float GetHealthRegen() => healthRegenRate;

    private void ShowRegenEffect()
    {
        // Show green "+X" numbers
        if (DamageNumberManager.Instance != null)
        {
            float regenPerTick = healthRegenRate * regenEffectInterval;
            Vector3 effectPos = transform.position + Vector3.up * 0.5f;

            // If you have a ShowHeal method, use it here
            // Otherwise, consider extending DamageNumberManager for healing effects
        }
    }

    public bool IsNoHitViable() => !isHited;
}