using UnityEngine;

public class FootstepParticles : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float particleSpawnRate = 0.15f;
    [SerializeField] private int particlesPerStep = 3; // Increased

    [Header("Visual")]
    [SerializeField] private Color sandColor = new Color(0.9f, 0.8f, 0.6f, 0.9f); // More opaque
    [SerializeField] private float particleMinSize = 0.15f; // Increased
    [SerializeField] private float particleMaxSize = 0.3f;  // Increased

    private Rigidbody2D rb;
    private Vector3 lastPosition;
    private float lastSpawnTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lastPosition = transform.position;
    }

    void Update()
    {
        // Determine speed based on position change
        float speed = 0f;

        if (rb != null)
        {
            speed = rb.linearVelocity.magnitude;
        }

        // If Rigidbody speed is near zero, calculate by position change
        if (speed < 0.1f)
        {
            Vector3 deltaPosition = transform.position - lastPosition;
            speed = deltaPosition.magnitude / Time.deltaTime;
        }

        lastPosition = transform.position;

        // If the character moves fast enough
        if (speed > minSpeed)
        {
            if (Time.time >= lastSpawnTime + particleSpawnRate)
            {
                SpawnFootstepParticles();
                lastSpawnTime = Time.time;
            }
        }
    }

    private void SpawnFootstepParticles()
    {
        for (int i = 0; i < particlesPerStep; i++)
        {
            GameObject particle = new GameObject("FootstepParticle");

            // Greater spread
            Vector2 offset = Random.insideUnitCircle * 0.25f;
            particle.transform.position = transform.position + new Vector3(offset.x, offset.y - 0.3f, 0);

            FootstepParticle particleScript = particle.AddComponent<FootstepParticle>();

            // Bright color variation
            Color color = sandColor;
            color.r += Random.Range(-0.1f, 0.1f);
            color.g += Random.Range(-0.1f, 0.1f);
            color.b += Random.Range(-0.1f, 0.1f);
            color.a = Random.Range(0.7f, 0.9f); // More opaque

            float size = Random.Range(particleMinSize, particleMaxSize);

            particleScript.Initialize(color, size);
        }
    }
}