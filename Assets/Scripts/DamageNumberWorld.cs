using UnityEngine;
using TMPro;

public class DamageNumberWorld : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float randomSpread = 0.3f;

    private TextMeshPro textMesh;
    private float elapsed = 0f;
    private Vector3 velocity;
    private bool isInitialized = false;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();

        if (textMesh == null)
        {
            DebugHelper.LogError("DamageNumberWorld: TextMeshPro component not found!");
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Move upward
        transform.position += velocity * Time.deltaTime;

        // Deceleration
        velocity *= 0.97f;

        // Scale - starts small, grows, then shrinks
        float scale = 1f;
        if (t < 0.2f)
        {
            // Appearing - grows
            scale = Mathf.Lerp(0.5f, 1.2f, t / 0.2f);
        }
        else if (t < 0.4f)
        {
            // Stabilization
            scale = Mathf.Lerp(1.2f, 1f, (t - 0.2f) / 0.2f);
        }
        else if (t > 0.8f)
        {
            // Disappearing - shrinks
            scale = Mathf.Lerp(1f, 0.8f, (t - 0.8f) / 0.2f);
        }

        transform.localScale = Vector3.one * scale;

        // Fade out
        if (textMesh != null)
        {
            Color color = textMesh.color;

            // Start fading after 70% of time
            if (t > 0.7f)
            {
                color.a = 1f - ((t - 0.7f) / 0.3f);
            }

            textMesh.color = color;
        }

        // Rotate to camera (billboard)
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void Initialize(float damage, bool isCrit, Vector3 worldPosition)
    {
        transform.position = worldPosition + Vector3.up * 0.5f;

        float randomX = Random.Range(-randomSpread, randomSpread);
        float randomY = Random.Range(0.8f, 1.2f);
        velocity = new Vector3(randomX, moveSpeed * randomY, 0);

        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        if (textMesh != null)
        {
            int damageInt = Mathf.RoundToInt(damage);

            if (isCrit)
            {
                // Critical damage - yellow, large, with exclamation
                textMesh.text = damageInt.ToString() + "!";
                textMesh.fontSize = 5f;
                textMesh.color = Color.yellow;
                textMesh.fontStyle = FontStyles.Bold;

                // Additional outline for crits
                textMesh.outlineWidth = 0.2f;
                textMesh.outlineColor = new Color(0.8f, 0.4f, 0f, 1f); // Orange outline
            }
            else
            {
                // Normal damage - different colors depending on magnitude
                textMesh.text = damageInt.ToString();
                textMesh.fontSize = 3.5f;
                textMesh.fontStyle = FontStyles.Normal;
                textMesh.outlineWidth = 0.15f;
                textMesh.outlineColor = new Color(0f, 0f, 0f, 0.8f); // Black outline

                // Color depends on damage
                if (damage >= 100)
                {
                    // Very strong hit - red
                    textMesh.color = new Color(1f, 0.2f, 0.2f);
                }
                else if (damage >= 50)
                {
                    // Strong hit - orange
                    textMesh.color = new Color(1f, 0.6f, 0f);
                }
                else if (damage >= 20)
                {
                    // Medium hit - white
                    textMesh.color = Color.white;
                }
                else if (damage >= 10)
                {
                    // Weak hit - light gray
                    textMesh.color = new Color(0.9f, 0.9f, 0.9f);
                }
                else
                {
                    // Very weak - gray
                    textMesh.color = new Color(0.6f, 0.6f, 0.6f);
                }
            }

            // DebugHelper.Log($"DamageNumber: {textMesh.text}, color: {textMesh.color}");
        }

        isInitialized = true;
        elapsed = 0f;
    }
}