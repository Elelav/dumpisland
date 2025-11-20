using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Items/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Information")]
    public string weaponName = "Weapon";
    public Sprite icon;

    [Header("Attack Type")]
    public AttackType attackType = AttackType.Circle; // NEW!
    public float attackAngle = 360f; // Attack angle (for cone attacks)
    public Vector2 rectangleSize = new Vector2(1, 3); // Size for rectangle attacks

    [Header("Base Stats (Level 1)")]
    public float baseDamage = 10f;
    public float baseAttackSpeed = 1f;
    public float baseAttackRange = 3f;
    public float baseCritChance = 0.05f;
    public float baseCritMultiplier = 2f;

    [Header("Per Level Growth")]
    public float damagePerLevel = 2f;
    public float attackSpeedPerLevel = 0.1f;
    public float rangePerLevel = 0.2f;
    public float critChancePerLevel = 0.01f;
    public float critMultiplierPerLevel = 0.1f;

    [Header("Visuals and Sound")]
    public GameObject projectilePrefab;
    public GameObject meleeEffectPrefab;
    public float projectileSpeed = 10f;
    public float effectSizeMultiplier = 1f;
    public Color weaponColor = Color.white;

    [Header("Attack Sounds")]
    public AudioClip attackSound; // Single sound (priority if filled)
    public AudioClip[] attackSounds; // Array for variety
    [Range(0f, 1f)] public float attackVolume = 0.4f; // Volume
    [Range(-0.3f, 0.3f)] public float pitchVariation = 0.1f; // Pitch variation

    [Header("Animated Effects")]
    public Sprite[] meleeAnimationFrames; // Sprite array for animation
    public float meleeAnimationFPS = 12f; // Animation speed (frames/sec)
    public bool useAnimatedEffect = false; // Use animation instead of prefab?
    public SpriteDirection spriteBaseDirection = SpriteDirection.South;

    // Other methods unchanged...
    public float GetDamage(int level)
    {
        return baseDamage + (damagePerLevel * (level - 1));
    }

    public float GetAttackSpeed(int level)
    {
        return baseAttackSpeed + (attackSpeedPerLevel * (level - 1));
    }

    public float GetRange(int level)
    {
        float baseRange = baseAttackRange + (rangePerLevel * (level - 1));

        // Range multiplier from perks (only for melee weapons)
        if (projectilePrefab == null) // Only melee weapons
        {
            PlayerPerks perks = GameObject.FindObjectOfType<PlayerPerks>();
            if (perks != null)
            {
                baseRange *= perks.GetTotalStatMultiplier("attackRange");
            }
        }

        return baseRange;
    }

    public float GetCritChance(int level)
    {
        return Mathf.Min(baseCritChance + (critChancePerLevel * (level - 1)), 1f);
    }

    public float GetCritMultiplier(int level)
    {
        return baseCritMultiplier + (critMultiplierPerLevel * (level - 1));
    }

    public float GetAttackCooldown(int level)
    {
        float baseSpeed = GetAttackSpeed(level);

        // Attack speed multiplier from perks
        PlayerPerks perks = GameObject.FindObjectOfType<PlayerPerks>();
        if (perks != null)
        {
            baseSpeed *= perks.GetTotalStatMultiplier("attackSpeed");
        }

        return 1f / baseSpeed;
    }
}

// NEW ENUM - attack types
public enum AttackType
{
    Circle,      // Circular (360°) - for staffs
    Cone,        // Cone (sword, axe)
    TargetAOE,   // Around target (hammer)
    Rectangle,   // Rectangle (spear)
    Projectile   // Projectile
}

// Effect sprite base direction
public enum SpriteDirection
{
    North,  // Up (↑)
    South,  // Down (↓)
    East,   // Right (→)
    West    // Left (←)
}