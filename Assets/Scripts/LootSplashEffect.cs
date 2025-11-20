using UnityEngine;

public class LootSplashEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int particleCount = 15;
    [SerializeField] private Color splashColor = new Color(0.7f, 0.9f, 1f, 0.9f);
    [SerializeField] private float minSize = 0.1f;
    [SerializeField] private float maxSize = 0.25f;
    [SerializeField] private float explosionForce = 5f;

    void Start()
    {
        CreateSplash();

        // Destroy the object itself after 2 seconds
        Destroy(gameObject, 2f);
    }

    private void CreateSplash()
    {
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject("SplashParticle");
            particle.transform.position = transform.position;

            WaterSplashParticle splash = particle.AddComponent<WaterSplashParticle>();

            // Random direction upward and sideways
            Vector2 direction = new Vector2(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1f) // More upward
            ).normalized;

            float speed = Random.Range(explosionForce * 0.5f, explosionForce);

            // Color variation
            Color color = splashColor;
            color.r += Random.Range(-0.1f, 0.1f);
            color.g += Random.Range(-0.1f, 0.1f);
            color.b += Random.Range(-0.05f, 0.05f);

            float size = Random.Range(minSize, maxSize);

            splash.Initialize(direction * speed, color, size);
        }
    }
}