using UnityEngine;
using System.Collections;

// Special weapon class for flamethrower
[System.Serializable]
public class FlameThrowerWeapon : Weapon
{
    [Header("Flamethrower")]
    private bool isFiring = false;
    private float lastFireTime = -999f;

    // Stream settings
    private const float FIRE_DURATION = 1.5f; // Stream duration
    private const float COOLDOWN = 3f; // Cooldown after firing
    private const float CONE_ANGLE = 30f; // Cone angle
    private const int PROJECTILES_PER_SECOND = 30; // Projectiles per second

    // Prefab
    private static GameObject flameProjectilePrefab;

    public FlameThrowerWeapon(WeaponData weaponData) : base(weaponData)
    {
    }

    // Override attack method
    public void AttackFlameThrower(Transform attacker, Transform target)
    {
        // Check cooldown
        if (Time.time < lastFireTime + COOLDOWN)
        {
            return;
        }

        // Check if already firing
        if (isFiring)
        {
            return;
        }

        // Start firing
        MonoBehaviour mono = attacker.GetComponent<MonoBehaviour>();
        if (mono != null)
        {
            mono.StartCoroutine(FireFlame(attacker, target));
        }
    }

    private IEnumerator FireFlame(Transform attacker, Transform target)
    {
        isFiring = true;
        lastFireTime = Time.time;

        DebugHelper.Log("FLAMETHROWER: Firing started!");

        // Load prefab
        if (flameProjectilePrefab == null)
        {
            flameProjectilePrefab = Resources.Load<GameObject>("Projectiles/FlameProjectile");

            if (flameProjectilePrefab == null)
            {
                DebugHelper.LogError("FlameParticle prefab not found!");
                isFiring = false;
                yield break;
            }
        }

        // Get perks
        PlayerPerks playerPerks = attacker.GetComponent<PlayerPerks>();

        // Calculate damage
        float totalDamage = data.GetDamage(level);

        // Apply perks to damage
        if (playerPerks != null)
        {
            totalDamage *= playerPerks.GetTotalStatMultiplier("damage");
            totalDamage *= playerPerks.GetTotalStatMultiplier("meleeDamage");
        }

        // Total projectiles count
        int totalProjectiles = Mathf.RoundToInt(PROJECTILES_PER_SECOND * FIRE_DURATION);

        // Damage per projectile
        float damagePerProjectile = totalDamage / totalProjectiles;

        // Interval between shots
        float shootInterval = FIRE_DURATION / totalProjectiles;

        // Range
        float range = data.GetRange(level);
        if (playerPerks != null)
        {
            range *= playerPerks.GetTotalStatMultiplier("attackRange");
        }

        // Projectile speed
        float projectileSpeed = range / 0.6f; // Should reach in 0.6 sec (lifetime)

        // Fire projectiles
        for (int i = 0; i < totalProjectiles; i++)
        {
            // Direction to nearest enemy (update each tick)
            Vector2 baseDirection;
            Transform nearestEnemy = FindNearestEnemyInRange(attacker.position, range);

            if (nearestEnemy != null)
            {
                baseDirection = (nearestEnemy.position - attacker.position).normalized;
            }
            else if (target != null)
            {
                baseDirection = (target.position - attacker.position).normalized;
            }
            else
            {
                baseDirection = Vector2.right; // Right by default
            }

            // Random spread in cone
            float randomAngle = Random.Range(-CONE_ANGLE / 2f, CONE_ANGLE / 2f);
            float radians = randomAngle * Mathf.Deg2Rad;

            Vector2 direction = new Vector2(
                baseDirection.x * Mathf.Cos(radians) - baseDirection.y * Mathf.Sin(radians),
                baseDirection.x * Mathf.Sin(radians) + baseDirection.y * Mathf.Cos(radians)
            );

            // Create projectile
            GameObject projectileObj = Object.Instantiate(
                flameProjectilePrefab,
                attacker.position,
                Quaternion.identity
            );

            FlameProjectile flame = projectileObj.GetComponent<FlameProjectile>();
            if (flame != null)
            {
                flame.Initialize(direction, damagePerProjectile, projectileSpeed, data.weaponName);
            }

            // Sound (every 5th projectile to avoid spam)
            if (i % 5 == 0 && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySwordAttack(); // Can be replaced with fire sound
            }

            yield return new WaitForSeconds(shootInterval);
        }

        DebugHelper.Log("FLAMETHROWER: Firing finished!");

        isFiring = false;
    }

    // Find nearest enemy in range
    private Transform FindNearestEnemyInRange(Vector3 position, float range)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float minDistance = range;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    // Check if weapon is ready
    public bool IsReady()
    {
        return !isFiring && Time.time >= lastFireTime + COOLDOWN;
    }

    // Get cooldown progress (for UI)
    public float GetCooldownProgress()
    {
        if (isFiring) return 0f;

        float timeSinceLastFire = Time.time - lastFireTime;
        if (timeSinceLastFire >= COOLDOWN) return 1f;

        return timeSinceLastFire / COOLDOWN;
    }
}