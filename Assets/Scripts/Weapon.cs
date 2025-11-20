using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon
{
    public WeaponData data;
    public int level = 1;

    private float lastAttackTime = 0f;
    private int attackCounter = 0;

    private List<GameObject> activeOrbitingProjectiles = new List<GameObject>();
    private float orbitalCooldownEndTime = 0f;

    public Weapon(WeaponData weaponData)
    {
        data = weaponData;
        level = 1;
    }

    public bool CanLevelUp()
    {
        return level < 10;
    }

    public void LevelUp()
    {
        if (CanLevelUp())
        {
            level++;
            DebugHelper.Log($"{data.weaponName} leveled up to level {level}!");
        }
    }

    public bool CanAttack()
    {
        if (data == null) return false;
        return Time.time >= lastAttackTime + data.GetAttackCooldown(level);
    }

    // UPDATED ATTACK METHOD
    public void Attack(Transform attacker, Transform primaryTarget)
    {
        if (!CanAttack()) return;

        lastAttackTime = Time.time;

        float distance = Vector2.Distance(attacker.position, primaryTarget.position);
        if (distance > data.GetRange(level))
        {
            return;
        }

        // Calculate damage
        float baseDamage = data.GetDamage(level);

        // Apply perk multipliers
        PlayerPerks playerPerks = attacker.GetComponent<PlayerPerks>();
        if (playerPerks != null)
        {
            // General damage
            baseDamage *= playerPerks.GetTotalStatMultiplier("damage");

            // Specific damage (melee/ranged)
            if (data.projectilePrefab != null)
            {
                baseDamage *= playerPerks.GetTotalStatMultiplier("rangedDamage");
            }
            else
            {
                baseDamage *= playerPerks.GetTotalStatMultiplier("meleeDamage");
            }
        }

        float damage = baseDamage;

        // Check for crit
        float critChance = data.GetCritChance(level);

        // Bonus crit chance from perks
        if (playerPerks != null)
        {
            critChance += playerPerks.GetTotalBonus("critChance");
        }

        bool isCrit = Random.value <= critChance;

        if (isCrit)
        {
            float critMultiplier = data.GetCritMultiplier(level);

            // Bonus crit damage from perks
            if (playerPerks != null)
            {
                critMultiplier += playerPerks.GetTotalBonus("critMultiplier");
            }

            damage *= critMultiplier;

            DebugHelper.Log($"CRITICAL HIT! x{critMultiplier}");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCrit();
            }
        }

        // Direction to target
        Vector2 direction = (primaryTarget.position - attacker.position).normalized;

        // CHECK: Projectile or melee?
        if (data.projectilePrefab != null)
        {
            // RANGED - shoot projectile
            ShootProjectile(attacker, primaryTarget, damage, isCrit, direction);
        }
        else
        {
            // MELEE - AOE attack
            PerformMeleeAttack(attacker, damage, isCrit, direction);
        }

        // Attack sound
        PlayAttackSound();
    }

    // NEW METHOD: Melee attack with AOE
    private void PerformMeleeAttack(Transform attacker, float damage, bool isCrit, Vector2 direction)
    {
        // Increment attack counter
        attackCounter++;

        // Check for Shockwave (every 10th attack)
        bool isShockwave = false;
        PlayerPerks playerPerks = attacker.GetComponent<PlayerPerks>();

        if (playerPerks != null && playerPerks.HasPerk("knockback"))
        {
            if (attackCounter >= 10)
            {
                isShockwave = true;
                attackCounter = 0;
                DebugHelper.Log("🌊 SHOCKWAVE!");
            }
        }

        // Process shockwave first (if exists)
        if (isShockwave)
        {
            PerformShockwave(attacker, damage);
            // Wave already dealt damage and knocked back everyone - we can return
            return;
        }

        // Regular attack (no shockwave)
        float attackRange = data.GetRange(level);
        Collider2D[] hits;
        int hitCount = 0;
        Vector3 effectPosition = attacker.position;

        switch (data.attackType)
        {
            case AttackType.Circle:
                hits = Physics2D.OverlapCircleAll(attacker.position, attackRange);
                hitCount = ProcessHits(hits, damage, isCrit, attacker.position, direction, false);
                effectPosition = attacker.position;
                CreateMeleeEffect(effectPosition, direction, attackRange);
                break;

            case AttackType.Cone:
                hits = Physics2D.OverlapCircleAll(attacker.position, attackRange);
                hitCount = ProcessConeHits(hits, damage, isCrit, attacker.position, direction, data.attackAngle, false);
                effectPosition = attacker.position;

                if (data.meleeEffectPrefab != null)
                {
                    CreateMeleeEffect(effectPosition, direction, attackRange);
                }
                else
                {
                    CreateConeEffect(effectPosition, direction, attackRange, data.attackAngle);
                }
                break;

            case AttackType.TargetAOE:
                Transform target = FindNearestEnemyInRange(attacker.position, attackRange);
                if (target != null)
                {
                    effectPosition = target.position;
                    hits = Physics2D.OverlapCircleAll(effectPosition, attackRange * 0.7f);
                    hitCount = ProcessHits(hits, damage, isCrit, effectPosition, direction, false);
                    CreateMeleeEffect(effectPosition, direction, attackRange * 0.7f);
                }
                break;

            case AttackType.Rectangle:
                Vector2 boxCenter = (Vector2)attacker.position + direction * (data.rectangleSize.y / 2);
                hits = Physics2D.OverlapBoxAll(boxCenter, data.rectangleSize, Vector2.SignedAngle(Vector2.up, direction));
                hitCount = ProcessHits(hits, damage, isCrit, boxCenter, direction, false);
                effectPosition = attacker.position;

                if (data.meleeEffectPrefab != null)
                {
                    CreateMeleeEffect(effectPosition, direction, data.rectangleSize.y / 2f);
                }
                else
                {
                    CreateRectangleEffect(effectPosition, direction, data.rectangleSize);
                }
                break;
        }

        DebugHelper.Log($"Melee attack ({data.attackType}): hits {hitCount}");

        if (hitCount > 0)
        {
            GameStats stats = GameObject.FindObjectOfType<GameStats>();
            if (stats != null)
            {
                float totalDamage = damage * hitCount;
                stats.AddDamageDealt(damage * hitCount);
                stats.AddWeaponDamage(data.weaponName, totalDamage);
            }
        }
    }

    // Shockwave - always circular, regardless of weapon type
    private void PerformShockwave(Transform attacker, float damage)
    {
        // FIXED shockwave radius
        float shockwaveRadius = 5f;

        // Create visual effect FIRST (to see the radius)
        CreateShockwaveEffect(attacker.position, shockwaveRadius);

        // Find ALL enemies in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(attacker.position, shockwaveRadius);

        int hitCount = 0;

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    // Deal damage
                    enemyHealth.TakeDamage(damage, false); // Wave doesn't crit
                    hitCount++;

                    // Knockback
                    ApplyKnockback(hit.gameObject, attacker.position);

                    DebugHelper.Log($"🌊 Wave: damage {damage:F0} + knockback {hit.name}");
                }
            }
        }

        DebugHelper.Log($"🌊 SHOCKWAVE: hit {hitCount} enemies (radius {shockwaveRadius})");

        // Update statistics
        if (hitCount > 0)
        {
            GameStats stats = GameObject.FindObjectOfType<GameStats>();
            if (stats != null)
            {
                float totalDamage = damage * hitCount;
                stats.AddDamageDealt(totalDamage);
                stats.AddWeaponDamage(data.weaponName + " (Wave)", totalDamage);
            }
        }
    }

    // Hit processing (regular)
    private int ProcessHits(Collider2D[] hits, float damage, bool isCrit, Vector3 effectPosition, Vector2 direction, bool applyKnockback = false)
    {
        int hitCount = 0;

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage, isCrit);
                    hitCount++;

                    // ✅ NEW: Knockback
                    if (applyKnockback)
                    {
                        ApplyKnockback(hit.gameObject, effectPosition);
                    }
                }
            }
        }

        return hitCount;
    }

    // Hit processing with angle check (for cone)
    private int ProcessConeHits(Collider2D[] hits, float damage, bool isCrit, Vector3 origin, Vector2 direction, float coneAngle, bool applyKnockback = false)
    {
        int hitCount = 0;

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                // Check angle to enemy
                Vector2 toEnemy = (hit.transform.position - origin).normalized;
                float angle = Vector2.Angle(direction, toEnemy);

                // If enemy is in cone
                if (angle <= coneAngle / 2f)
                {
                    EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(damage, isCrit);
                        hitCount++;

                        // ✅ NEW: Knockback
                        if (applyKnockback)
                        {
                            ApplyKnockback(hit.gameObject, origin);
                        }
                    }
                }
            }
        }

        return hitCount;
    }

    // Find nearest enemy in range (for hammer)
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

    // VISUAL MELEE EFFECT
    // UPDATED CreateMeleeEffect method - animation support
    private void CreateMeleeEffect(Vector3 position, Vector2 direction, float range)
    {
        GameObject effectPrefab = null;

        // Check: use animation or regular prefab?
        if (data.useAnimatedEffect && data.meleeAnimationFrames != null && data.meleeAnimationFrames.Length > 0)
        {
            // ANIMATED EFFECT
            CreateAnimatedMeleeEffect(position, direction, range);
            return;
        }

        // OLD WAY - regular prefab
        if (data.meleeEffectPrefab != null)
        {
            effectPrefab = data.meleeEffectPrefab;
        }
        else
        {
            effectPrefab = Resources.Load<GameObject>("Prefabs/MeleeEffect");
        }

        if (effectPrefab != null)
        {
            GameObject effect = Object.Instantiate(effectPrefab);

            float effectSize;

            SlashEffect slashEffect = effect.GetComponent<SlashEffect>();
            MeleeAttackEffect meleeEffect = effect.GetComponent<MeleeAttackEffect>();

            if (slashEffect != null)
            {
                effectSize = range;
                DebugHelper.Log($"SlashEffect: range={range}, effectSize={effectSize}");
                slashEffect.Initialize(position, direction, data.weaponColor, effectSize);
            }
            else if (meleeEffect != null)
            {
                effectSize = range * 2f * data.effectSizeMultiplier;
                DebugHelper.Log($"MeleeAttackEffect: range={range}, diameter={effectSize}");
                meleeEffect.Initialize(position, direction, data.weaponColor, effectSize);
            }
            else
            {
                DebugHelper.LogWarning($"Effect has no components!");
                effect.transform.position = position;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                effect.transform.rotation = Quaternion.Euler(0, 0, angle);
                effect.transform.localScale = Vector3.one * range * 2f;

                SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = data.weaponColor;
                }

                Object.Destroy(effect, 0.5f);
            }
        }
        else
        {
            CreateSimpleMeleeEffect(position, range);
        }
    }

    // FIXED METHOD - consider attack type for position
    private void CreateAnimatedMeleeEffect(Vector3 position, Vector2 direction, float range)
    {
        // Find attacker's Transform (player)
        Transform attackerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Adjust position based on attack type
        Vector3 finalPosition = position;

        switch (data.attackType)
        {
            case AttackType.Cone:
                float coneOffset = range * 0.5f;
                finalPosition = position + (Vector3)(direction * coneOffset);
                break;

            case AttackType.Rectangle:
                // range already contains half length
                finalPosition = position + (Vector3)(direction * range);
                break;

            case AttackType.Circle:
                // Circular attack - center on player
                finalPosition = position;
                break;

            case AttackType.TargetAOE:
                // ✅ For TargetAOE position is already correct (target.position)
                finalPosition = position;
                DebugHelper.Log($"💥 TargetAOE: hit position={finalPosition}");
                break;
        }

        // Create GameObject
        GameObject effectObj = new GameObject("AnimatedMeleeEffect");

        SpriteRenderer sr = effectObj.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 10;

        AnimatedMeleeEffect animEffect = effectObj.AddComponent<AnimatedMeleeEffect>();

        // Calculate optimal FPS
        float attackCooldown = data.GetAttackCooldown(level);
        int frameCount = data.meleeAnimationFrames.Length;
        float optimalFPS = frameCount / (attackCooldown * 0.9f);
        optimalFPS = Mathf.Clamp(optimalFPS, 8f, 30f);

        // For Rectangle use special method
        if (data.attackType == AttackType.Rectangle)
        {
            Vector2 effectSize = data.rectangleSize * data.effectSizeMultiplier;

            animEffect.InitializeRectangle(
                data.meleeAnimationFrames,
                finalPosition,
                direction,
                data.weaponColor,
                effectSize,
                optimalFPS,
                data.spriteBaseDirection
            );
        }
        else
        {
            // For Circle and TargetAOE - circular size
            // range is radius, multiply by 2 for diameter
            float effectSize = range * 2f * data.effectSizeMultiplier;

            DebugHelper.Log($"⭕ {data.attackType}: range={range}, effectSize={effectSize}");

            animEffect.Initialize(
                data.meleeAnimationFrames,
                finalPosition,
                direction,
                data.weaponColor,
                effectSize,
                optimalFPS,
                data.spriteBaseDirection
            );
        }

        // ✅ Attach to player ONLY if it's NOT TargetAOE
        if (attackerTransform != null && data.attackType != AttackType.TargetAOE)
        {
            effectObj.transform.SetParent(attackerTransform);
            DebugHelper.Log($"Effect attached to player");
        }
        else if (data.attackType == AttackType.TargetAOE)
        {
            DebugHelper.Log($"💥 TargetAOE effect stays at hit location");
        }
    }

    // Simple effect if no prefab
    private void CreateSimpleMeleeEffect(Vector3 position, float range)
    {
        GameObject effect = new GameObject("MeleeEffect");
        effect.transform.position = position;

        SpriteRenderer sr = effect.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("UI/Skin/Knob");
        sr.color = new Color(data.weaponColor.r, data.weaponColor.g, data.weaponColor.b, 0.5f);
        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 6;

        Object.Destroy(effect, 0.3f);

        // Animation
        MonoBehaviour mono = GameObject.FindObjectOfType<MonoBehaviour>();
        if (mono != null)
        {
            mono.StartCoroutine(SimpleMeleeAnimation(effect, range));
        }
    }

    private static System.Collections.IEnumerator SimpleMeleeAnimation(GameObject effect, float range)
    {
        SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration && effect != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            effect.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, range * 2f, t);

            Color color = sr.color;
            color.a = 0.5f * (1f - t);
            sr.color = color;

            yield return null;
        }
    }

    // Shoot projectile
    private void ShootProjectile(Transform attacker, Transform target, float damage, bool isCrit, Vector2 direction)
    {
        PlayerPerks playerPerks = attacker.GetComponent<PlayerPerks>();

        // ✅ CHECK: Is this orbital weapon?
        bool isOrbitalWeapon = false;
        if (data.projectilePrefab != null)
        {
            OrbitingProjectile testOrbit = data.projectilePrefab.GetComponent<OrbitingProjectile>();
            if (testOrbit != null)
            {
                isOrbitalWeapon = true;
            }
        }

        // ✅ ORBITAL WEAPON - NEW LOGIC WITH COOLDOWN
        if (isOrbitalWeapon)
        {
            // Clean up destroyed stars
            activeOrbitingProjectiles.RemoveAll(proj => proj == null);

            // ✅ NEW: Check cooldown
            if (Time.time < orbitalCooldownEndTime)
            {
                // Stars still on cooldown - do nothing
                return;
            }

            // ✅ NEW: If there are living stars - don't create new ones
            if (activeOrbitingProjectiles.Count > 0)
            {
                return;
            }

            // Calculate max stars
            int maxStars = 1;
            if (playerPerks != null)
            {
                maxStars += Mathf.RoundToInt(playerPerks.GetTotalBonus("projectileCount"));
            }

            // ✅ LIFETIME CALCULATION: 5 full revolutions
            // orbitSpeed from prefab (get from component)
            OrbitingProjectile orbitComponent = data.projectilePrefab.GetComponent<OrbitingProjectile>();
            float orbitSpeed = 180f; // Default

            if (orbitComponent != null)
            {
                // Get via reflection or SerializedField (if public)
                // Using default 180°/sec for now
                orbitSpeed = 180f;
            }

            // Time for 1 revolution = 360° / speed
            float timePerOrbit = 360f / orbitSpeed;
            float lifetime = timePerOrbit * 5f; // 5 revolutions

            DebugHelper.Log($"⭐ Creating stars: orbitSpeed={orbitSpeed}°/sec, 1 revolution={timePerOrbit:F1}s, lifetime={lifetime:F1}s (5 revolutions)");

            // ✅ CREATE ALL stars synchronously
            for (int i = 0; i < maxStars; i++)
            {
                GameObject projectileObj = Object.Instantiate(
                    data.projectilePrefab,
                    attacker.position,
                    Quaternion.identity
                );

                OrbitingProjectile orbitingProj = projectileObj.GetComponent<OrbitingProjectile>();
                if (orbitingProj != null)
                {
                    // Even angle distribution
                    float startAngle = (360f / maxStars) * i;

                    orbitingProj.Initialize(damage, isCrit, data.weaponColor, playerPerks, startAngle, data.weaponName, lifetime);

                    activeOrbitingProjectiles.Add(projectileObj);

                    DebugHelper.Log($"⭐ Created star {i + 1}/{maxStars}, angle {startAngle}°");
                }
            }

            // ✅ NEW: Start cooldown after creation
            // Cooldown = lifetime + weapon attack time (considering level)
            float cooldown = 5f - data.GetAttackSpeed(level);
            orbitalCooldownEndTime = cooldown;//Time.time + cooldown;

            DebugHelper.Log($"⏳ Orbital cooldown: {cooldown:F1}s (life {lifetime:F1}s + attack {data.GetAttackCooldown(level):F1}s)");

            return;
        }

        // ✅ REGULAR WEAPON - unchanged
        int projectileCount = 1;

        bool isVacuumBag = false;
        if (data.projectilePrefab != null)
        {
            VacuumBagProjectile testVacuum = data.projectilePrefab.GetComponent<VacuumBagProjectile>();
            if (testVacuum != null)
            {
                isVacuumBag = true;
                projectileCount = 1;
            }
        }

        if (!isVacuumBag && playerPerks != null)
        {
            projectileCount += Mathf.RoundToInt(playerPerks.GetTotalBonus("projectileCount"));
        }

        for (int i = 0; i < projectileCount; i++)
        {
            Vector2 shootDirection = direction;

            if (projectileCount > 1)
            {
                float spreadAngle = 40f;
                float angleStep = spreadAngle / (projectileCount - 1);
                float startAngleSpread = -spreadAngle / 2f;
                float currentAngle = startAngleSpread + (angleStep * i);

                float radians = currentAngle * Mathf.Deg2Rad;
                shootDirection = new Vector2(
                    direction.x * Mathf.Cos(radians) - direction.y * Mathf.Sin(radians),
                    direction.x * Mathf.Sin(radians) + direction.y * Mathf.Cos(radians)
                );
            }

            GameObject projectileObj = Object.Instantiate(
                data.projectilePrefab,
                attacker.position,
                Quaternion.identity
            );

            VacuumBagProjectile vacuumBag = projectileObj.GetComponent<VacuumBagProjectile>();
            if (vacuumBag != null)
            {
                vacuumBag.Initialize(shootDirection, damage, isCrit, playerPerks, data.weaponName);
            }
            else
            {
                BoomerangProjectile boomerangProj = projectileObj.GetComponent<BoomerangProjectile>();
                if (boomerangProj != null)
                {
                    boomerangProj.Initialize(shootDirection, damage, isCrit, data.weaponColor, data.projectileSpeed, playerPerks, data.weaponName);
                }
                else
                {
                    Projectile projectile = projectileObj.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        projectile.Initialize(shootDirection, damage, isCrit, data.weaponColor, data.projectileSpeed, playerPerks, data.weaponName);
                    }
                    else
                    {
                        ExplodingProjectile explodingProj = projectileObj.GetComponent<ExplodingProjectile>();
                        if (explodingProj != null)
                        {
                            explodingProj.Initialize(shootDirection, damage, isCrit, data.weaponColor, data.projectileSpeed, playerPerks, data.weaponName, damage * 0.5f);
                        }
                    }
                }
            }
        }
    }
    // Attack sound
    private void PlayAttackSound()
    {
        if (AudioManager.Instance == null) return;

        // PRIORITY 1: Use custom sound from WeaponData
        if (data.attackSound != null)
        {
            AudioManager.Instance.PlayWeaponSound(
                data.attackSound,
                data.attackVolume,
                data.pitchVariation
            );
            return;
        }

        // PRIORITY 2: Use sound array (random)
        if (data.attackSounds != null && data.attackSounds.Length > 0)
        {
            AudioClip randomClip = data.attackSounds[Random.Range(0, data.attackSounds.Length)];
            AudioManager.Instance.PlayWeaponSound(
                randomClip,
                data.attackVolume,
                data.pitchVariation
            );
            return;
        }

        // PRIORITY 3: Fallback to old system (by weapon name)
        if (data.weaponName.Contains("Гарпун") || data.weaponName.Contains("HarpoonPike") ||
            data.weaponName.Contains("Graple") || data.weaponName.Contains("Shovel"))
        {
            AudioManager.Instance.PlaySwordAttack();
        }
        else if (data.weaponName.Contains("Лук") || data.weaponName.Contains("Net") ||
                 data.weaponName.Contains("Арбалет") || data.weaponName.Contains("Crossbow"))
        {
            AudioManager.Instance.PlayBowAttack();
        }
        else
        {
            AudioManager.Instance.PlayBowAttack();
        }
    }

    // Crit effect (for visualization)
    private void CreateCritEffect(Vector3 position)
    {
        GameObject critEffect = new GameObject("CritEffect");
        critEffect.transform.position = position;

        SpriteRenderer sr = critEffect.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("UI/Skin/Knob");
        sr.color = new Color(1f, 1f, 0f, 0.8f);
        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 20;

        MonoBehaviour mono = GameObject.FindObjectOfType<MonoBehaviour>();
        if (mono != null)
        {
            mono.StartCoroutine(CritEffectAnimation(critEffect));
        }
    }

    private static System.Collections.IEnumerator CritEffectAnimation(GameObject effect)
    {
        SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            effect.transform.localScale = Vector3.one * (1f + t * 2f);

            Color color = sr.color;
            color.a = 1f - t;
            sr.color = color;

            yield return null;
        }

        Object.Destroy(effect);
    }

    // Old method for backward compatibility
    private void CreateAttackEffect(Vector3 from, Vector3 to)
    {
        Debug.DrawLine(from, to, data.weaponColor, 0.2f);
    }

    public string GetStatsText()
    {
        return $"{data.weaponName} (Lvl.{level})\n" +
               $"Damage: {data.GetDamage(level):F0}\n" +
               $"Speed: {data.GetAttackSpeed(level):F1} atk/sec\n" +
               $"Range: {data.GetRange(level):F1}\n" +
               $"Crit chance: {data.GetCritChance(level) * 100:F0}%\n" +
               $"Crit damage: x{data.GetCritMultiplier(level):F1}";
    }

    // Hitbox visualization in editor (for debugging)
    public void DrawAttackRange(Vector3 position, Vector3 direction)
    {
#if UNITY_EDITOR
        float range = data.GetRange(1); // Use base level for visualization

        if (data.projectilePrefab != null)
        {
            // For projectiles - show line
            UnityEngine.Debug.DrawRay(position, direction * range, data.weaponColor, 0.5f);
        }
        else
        {
            // For melee - show circle
            UnityEngine.Gizmos.color = new Color(data.weaponColor.r, data.weaponColor.g, data.weaponColor.b, 0.3f);
            UnityEngine.Gizmos.DrawWireSphere(position, range);
        }
#endif
    }

    // Cone attack effect
    private void CreateConeEffect(Vector3 position, Vector2 direction, float range, float angle)
    {
        // If there's custom prefab - use it
        if (data.meleeEffectPrefab != null)
        {
            GameObject effect = Object.Instantiate(data.meleeEffectPrefab, position, Quaternion.identity);

            MeleeAttackEffect meleeEffect = effect.GetComponent<MeleeAttackEffect>();
            if (meleeEffect != null)
            {
                meleeEffect.Initialize(position, direction, data.weaponColor, range * 2f);
            }
            else
            {
                Object.Destroy(effect, 0.3f);
            }
            return;
        }

        // Otherwise create simple effect
        CreateSimpleConeEffect(position, direction, range, angle);
    }

    // Simple cone effect
    private void CreateSimpleConeEffect(Vector3 position, Vector2 direction, float range, float angle)
    {
        // Create multiple particles in fan shape
        int particleCount = 5;
        float angleStep = angle / (particleCount - 1);
        float startAngle = -angle / 2f;

        for (int i = 0; i < particleCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);

            // Rotate direction by current angle
            float radians = currentAngle * Mathf.Deg2Rad;
            Vector2 particleDir = new Vector2(
                direction.x * Mathf.Cos(radians) - direction.y * Mathf.Sin(radians),
                direction.x * Mathf.Sin(radians) + direction.y * Mathf.Cos(radians)
            );

            CreateConeParticle(position, particleDir, range, i * 0.05f);
        }
    }

    // Single particle for cone
    private void CreateConeParticle(Vector3 position, Vector2 direction, float range, float delay)
    {
        GameObject particle = new GameObject("ConeParticle");
        particle.transform.position = position;

        SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();

        // ✅ FIXED: Use CACHE instead of creating texture!
        sr.sprite = SpriteCache.Instance.GetCircleSprite(32);
        sr.color = new Color(data.weaponColor.r, data.weaponColor.g, data.weaponColor.b, 0.6f);
        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 6;

        Object.Destroy(particle, 0.3f + delay);

        // Animation
        MonoBehaviour mono = GameObject.FindObjectOfType<MonoBehaviour>();
        if (mono != null)
        {
            mono.StartCoroutine(ConeParticleAnimation(particle, direction, range, delay));
        }
    }

    private static System.Collections.IEnumerator ConeParticleAnimation(GameObject particle, Vector2 direction, float range, float delay)
    {
        yield return new WaitForSeconds(delay);

        SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
        Vector3 startPos = particle.transform.position;
        Vector3 endPos = startPos + (Vector3)(direction * range);

        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration && particle != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            particle.transform.position = Vector3.Lerp(startPos, endPos, t);
            particle.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 0.8f, t);

            Color color = sr.color;
            color.a = 0.6f * (1f - t);
            sr.color = color;

            yield return null;
        }
    }

    // Rectangle attack effect
    private void CreateRectangleEffect(Vector3 position, Vector2 direction, Vector2 size)
    {
        // If there's custom prefab - use it
        if (data.meleeEffectPrefab != null)
        {
            GameObject effect = Object.Instantiate(data.meleeEffectPrefab, position, Quaternion.identity);

            MeleeAttackEffect meleeEffect = effect.GetComponent<MeleeAttackEffect>();
            if (meleeEffect != null)
            {
                meleeEffect.Initialize(position, direction, data.weaponColor, size.y);
            }
            else
            {
                Object.Destroy(effect, 0.3f);
            }
            return;
        }

        // Otherwise create simple rectangle
        CreateSimpleRectangleEffect(position, direction, size);
    }

    // Simple rectangle effect
    private void CreateSimpleRectangleEffect(Vector3 position, Vector2 direction, Vector2 size)
    {
        // Rectangle center offset by half length in attack direction
        Vector3 effectPosition = position + (Vector3)(direction.normalized * (size.y / 2f));

        GameObject effect = new GameObject("RectangleEffect");
        effect.transform.position = effectPosition;

        // Rotation FIRST, before creating sprite
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        effect.transform.rotation = Quaternion.Euler(0, 0, angle);

        SpriteRenderer sr = effect.AddComponent<SpriteRenderer>();

        // Create elongated rectangular sprite
        // size.x - width (across direction)
        // size.y - length (along direction)
        int width = 32;
        int height = 64;

        Texture2D texture = new Texture2D(width, height);
        Color fillColor = new Color(data.weaponColor.r, data.weaponColor.g, data.weaponColor.b, 0.5f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Can add gradient or border
                float edgeFade = 1f;

                // Darken edges
                if (x < 2 || x > width - 3) edgeFade = 0.5f;
                if (y < 2 || y > height - 3) edgeFade = 0.5f;

                Color pixelColor = fillColor;
                pixelColor.a *= edgeFade;

                texture.SetPixel(x, y, pixelColor);
            }
        }
        texture.Apply();

        sr.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32);
        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 6;

        // Scale to actual attack size
        // Important: scale applied AFTER rotation
        effect.transform.localScale = new Vector3(size.x, size.y, 1);

        Object.Destroy(effect, 0.3f);

        // Animation
        MonoBehaviour mono = GameObject.FindObjectOfType<MonoBehaviour>();
        if (mono != null)
        {
            mono.StartCoroutine(RectangleEffectAnimation(effect));
        }
    }

    private static System.Collections.IEnumerator RectangleEffectAnimation(GameObject effect)
    {
        SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
        Vector3 originalScale = effect.transform.localScale;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration && effect != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Small expansion at start
            if (t < 0.3f)
            {
                float scaleBoost = 1f + (t / 0.3f) * 0.2f;
                effect.transform.localScale = originalScale * scaleBoost;
            }

            // Fade out
            Color color = sr.color;
            color.a = 0.5f * (1f - t);
            sr.color = color;

            yield return null;
        }
    }

    // Enemy knockback
    private void ApplyKnockback(GameObject enemy, Vector3 fromPosition)
    {
        // Direction from hit point to enemy
        Vector2 knockbackDirection = (enemy.transform.position - fromPosition).normalized;

        // Knockback parameters
        float knockbackForce = 8f;
        float knockbackDuration = 0.3f;

        // Try to call method in EnemyAI
        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.ApplyKnockback(knockbackDirection, knockbackForce, knockbackDuration);
        }
        else
        {
            // Fallback for enemies without EnemyAI
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = knockbackDirection * knockbackForce;
                DebugHelper.LogWarning($"{enemy.name} knocked back (no EnemyAI, using Rigidbody2D)");
            }
        }
    }

    // Visual Shockwave effect
    // ✅ UPDATED METHOD: Shockwave visual effect (exact size)
    private void CreateShockwaveEffect(Vector3 position, float exactRadius)
    {
        GameObject shockwave = new GameObject("Shockwave");
        shockwave.transform.position = position;

        SpriteRenderer sr = shockwave.AddComponent<SpriteRenderer>();

        // Create circular texture
        int size = 128;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDist = distance / (size / 2f);

                // Double ring
                float alpha = 0f;

                // Outer bright ring
                if (normalizedDist > 0.85f && normalizedDist < 1f)
                {
                    alpha = 1f - Mathf.Abs(normalizedDist - 0.925f) * 6f;
                }

                // Inner ring
                if (normalizedDist > 0.7f && normalizedDist < 0.85f)
                {
                    alpha = Mathf.Max(alpha, (1f - Mathf.Abs(normalizedDist - 0.775f) * 6f) * 0.6f);
                }

                // Blue-white color
                texture.SetPixel(x, y, new Color(0.5f, 0.9f, 1f, alpha));
            }
        }
        texture.Apply();

        sr.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 2f);
        sr.sortingLayerName = "Enemies";
        sr.sortingOrder = 15;

        // Wave sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySwordAttack();
        }

        Object.Destroy(shockwave, 0.5f);

        // Pass EXACT radius to animation
        MonoBehaviour mono = GameObject.FindObjectOfType<MonoBehaviour>();
        if (mono != null)
        {
            mono.StartCoroutine(ShockwaveAnimation(shockwave, exactRadius));
        }
    }

    // Shockwave animation
    // Shockwave animation (exact size)
    private static System.Collections.IEnumerator ShockwaveAnimation(GameObject shockwave, float exactRadius)
    {
        SpriteRenderer sr = shockwave.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        float duration = 0.4f;

        // Start from 0 and expand to EXACT radius * 2 (diameter)
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * (exactRadius * 2f);

        DebugHelper.Log($"Wave: start scale={startScale}, end scale={endScale} (radius={exactRadius})");

        while (elapsed < duration && shockwave != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Expansion
            shockwave.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            // Fade out
            Color color = sr.color;
            color.a = 0.8f * (1f - t);
            sr.color = color;

            yield return null;
        }
    }
}