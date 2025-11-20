using UnityEngine;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float maxDetectionRange = 10f; // Maximum enemy detection range

    private PlayerInventory inventory;
    private List<Weapon> weapons;

    void Start()
    {
        inventory = GetComponent<PlayerInventory>();

        // Subscribe to inventory updates
        inventory.OnInventoryChanged.AddListener(OnInventoryUpdated);

        weapons = inventory.GetWeapons();
    }

    void Update()
    {
        // Auto-attack with all weapons independently
        AttackWithAllWeapons();
    }

    private void OnInventoryUpdated(List<Weapon> updatedWeapons)
    {
        weapons = updatedWeapons;
    }

    private void AttackWithAllWeapons()
    {
        if (weapons == null || weapons.Count == 0) return;

        foreach (Weapon weapon in weapons)
        {
            if (weapon == null) continue;

            // Check for flamethrower
            FlameThrowerWeapon flameThrower = weapon as FlameThrowerWeapon;
            if (flameThrower != null)
            {
                // Flamethrower has special logic
                if (flameThrower.IsReady())
                {
                    Transform target = FindNearestEnemy(weapon.data.GetRange(weapon.level));
                    if (target != null)
                    {
                        flameThrower.AttackFlameThrower(transform, target);
                    }
                }
            }
            else
            {
                // Regular weapon logic
                if (weapon.CanAttack())
                {
                    Transform target = FindNearestEnemy(weapon.data.GetRange(weapon.level));
                    if (target != null)
                    {
                        weapon.Attack(transform, target);
                    }
                }
            }
        }
    }

    private Transform FindNearestEnemy(float range)
    {
        // Find all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        Transform nearest = null;
        float minDistance = range; // Use weapon range for limit

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    // Visualize attack range in the editor
    void OnDrawGizmosSelected()
    {
        if (weapons == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Vector3 directionToEnemy = Vector3.right;
        Transform nearestEnemy = null;

        if (enemies.Length > 0)
        {
            float minDist = float.MaxValue;
            foreach (GameObject enemy in enemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestEnemy = enemy.transform;
                }
            }

            if (nearestEnemy != null)
            {
                directionToEnemy = (nearestEnemy.position - transform.position).normalized;
            }
        }

        foreach (Weapon weapon in weapons)
        {
            if (weapon != null && weapon.data != null)
            {
                float range = weapon.data.GetRange(weapon.level);
                Color gizmoColor = new Color(weapon.data.weaponColor.r, weapon.data.weaponColor.g, weapon.data.weaponColor.b, 0.3f);

                // Draw hitbox (attack radius)
                switch (weapon.data.attackType)
                {
                    case AttackType.Circle:
                        Gizmos.color = gizmoColor;
                        Gizmos.DrawWireSphere(transform.position, range);

                        // Additionally: show effect size (diameter)
                        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.15f);
                        Gizmos.DrawWireSphere(transform.position, range * weapon.data.effectSizeMultiplier);
                        break;

                    case AttackType.Cone:
                        DrawConeGizmo(transform.position, directionToEnemy, range, weapon.data.attackAngle, gizmoColor);
                        break;

                    case AttackType.TargetAOE:
                        if (nearestEnemy != null)
                        {
                            // Hitbox
                            Gizmos.color = gizmoColor;
                            Gizmos.DrawWireSphere(nearestEnemy.position, range * 0.7f);

                            // Effect size
                            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.15f);
                            float effectRadius = range * 0.7f * weapon.data.effectSizeMultiplier;
                            Gizmos.DrawWireSphere(nearestEnemy.position, effectRadius);

                            // Line to target
                            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f);
                            Gizmos.DrawLine(transform.position, nearestEnemy.position);
                        }
                        break;

                    case AttackType.Rectangle:
                        DrawRectangleGizmo(transform.position, directionToEnemy, weapon.data.rectangleSize, gizmoColor);
                        break;
                }

#if UNITY_EDITOR
                // Draw labels with weapon info
                string label = $"{weapon.data.weaponName}\n" +
                              $"Type: {weapon.data.attackType}\n" +
                              $"Range: {range:F1}\n" +
                              $"Effect: x{weapon.data.effectSizeMultiplier:F1}";

                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * (range + 1f),
                    label,
                    new GUIStyle() { normal = new GUIStyleState() { textColor = weapon.data.weaponColor } }
                );
#endif
            }
        }
    }

    // Draw cone gizmo
    private void DrawConeGizmo(Vector3 origin, Vector3 direction, float range, float angle, Color color)
    {
        Gizmos.color = color;

        int segments = 20;
        float angleStep = angle / segments;
        float startAngle = -angle / 2f;

        Vector3 previousPoint = origin;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            float radians = currentAngle * Mathf.Deg2Rad;

            Vector2 dir2D = new Vector2(direction.x, direction.y);
            Vector2 rotatedDir = new Vector2(
                dir2D.x * Mathf.Cos(radians) - dir2D.y * Mathf.Sin(radians),
                dir2D.x * Mathf.Sin(radians) + dir2D.y * Mathf.Cos(radians)
            );

            Vector3 point = origin + (Vector3)(rotatedDir.normalized * range);

            if (i > 0)
            {
                Gizmos.DrawLine(previousPoint, point);
            }

            Gizmos.DrawLine(origin, point);
            previousPoint = point;
        }
    }

    // Draw rectangle gizmo
    private void DrawRectangleGizmo(Vector3 origin, Vector3 direction, Vector2 size, Color color)
    {
        Gizmos.color = color;

        // Center of rectangle
        Vector3 center = origin + (direction * (size.y / 2f));

        // Perpendicular direction
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);

        // Four corners of rectangle
        Vector3 topLeft = center + (direction * size.y / 2f) + (perpendicular * size.x / 2f);
        Vector3 topRight = center + (direction * size.y / 2f) - (perpendicular * size.x / 2f);
        Vector3 bottomLeft = center - (direction * size.y / 2f) + (perpendicular * size.x / 2f);
        Vector3 bottomRight = center - (direction * size.y / 2f) - (perpendicular * size.x / 2f);

        // Draw borders
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);

        // Draw diagonals for clarity
        Gizmos.color = new Color(color.r, color.g, color.b, color.a * 0.5f);
        Gizmos.DrawLine(topLeft, bottomRight);
        Gizmos.DrawLine(topRight, bottomLeft);
    }
}