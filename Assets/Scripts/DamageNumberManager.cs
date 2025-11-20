using UnityEngine;

public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance;

    [Header("Prefab")]
    [SerializeField] private GameObject damageNumberPrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (damageNumberPrefab == null)
        {
            DebugHelper.LogError("DamageNumberManager: prefab not assigned!");
        }
    }

    public void ShowDamage(float damage, Vector3 worldPosition, bool isCrit = false)
    {
        if (damageNumberPrefab == null)
        {
            DebugHelper.LogWarning("DamageNumber prefab not assigned!");
            return;
        }

        DebugHelper.Log($"ShowDamage called: damage={damage}, position={worldPosition}, crit={isCrit}");

        GameObject damageObj = Instantiate(damageNumberPrefab, worldPosition, Quaternion.identity);

        if (damageObj == null)
        {
            DebugHelper.LogError("Failed to create damage number object!");
            return;
        }

        DamageNumberWorld damageNumber = damageObj.GetComponent<DamageNumberWorld>();
        if (damageNumber != null)
        {
            damageNumber.Initialize(damage, isCrit, worldPosition);
        }
        else
        {
            DebugHelper.LogError("Created object doesn't have DamageNumberWorld component!");
        }
    }
}