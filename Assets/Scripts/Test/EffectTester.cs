using UnityEngine;

public class EffectTester : MonoBehaviour
{
    [Header("Effects to Test")]
    [SerializeField] private GameObject swordSlashPrefab;
    [SerializeField] private GameObject hammerImpactPrefab;

    void Update()
    {
        // Test SwordSlash - J key
        if (Input.GetKeyDown(KeyCode.J))
        {
            TestEffect(swordSlashPrefab, "SwordSlash");
        }

        // Test HammerImpact - H key
        if (Input.GetKeyDown(KeyCode.H))
        {
            TestEffect(hammerImpactPrefab, "HammerImpact");
        }
    }

    private void TestEffect(GameObject prefab, string name)
    {
        if (prefab == null)
        {
            DebugHelper.LogError($"{name} prefab not assigned!");
            return;
        }

        DebugHelper.Log($"=== TEST {name} ===");

        // Find nearest enemy
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Vector3 targetPos = transform.position + Vector3.right * 3; // Right by default
        Vector3 direction = Vector3.right;

        if (enemies.Length > 0)
        {
            targetPos = enemies[0].transform.position;
            direction = (targetPos - transform.position).normalized;
        }

        DebugHelper.Log($"Creating {name} at position {targetPos}, direction {direction}");

        // Create effect
        GameObject effect = Instantiate(prefab);

        DebugHelper.Log($"Effect created at position: {effect.transform.position}");

        // Initialize
        MeleeAttackEffect meleeEffect = effect.GetComponent<MeleeAttackEffect>();
        if (meleeEffect != null)
        {
            DebugHelper.Log("MeleeAttackEffect found, initializing...");
            meleeEffect.Initialize(targetPos, direction, Color.cyan, 3f);
            DebugHelper.Log($"After Initialize position: {effect.transform.position}");
        }
        else
        {
            DebugHelper.LogError($"{name} doesn't have MeleeAttackEffect component!");
        }
    }
}