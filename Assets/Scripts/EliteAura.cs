using UnityEngine;

public class EliteAura : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minScale = 1.2f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private float rotationSpeed = 30f;

    private Vector3 baseScale;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        baseScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Size pulsing
        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        transform.localScale = baseScale * scale;

        // Rotation
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // Transparency pulsing
        Color color = spriteRenderer.color;
        color.a = Mathf.Lerp(0.3f, 0.6f, (Mathf.Sin(Time.time * pulseSpeed * 1.5f) + 1f) / 2f);
        spriteRenderer.color = color;
    }
}