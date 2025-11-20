using UnityEngine;
using System.Collections;

public class WaterWaveEffect : MonoBehaviour
{
    [Header("Animation settings")]
    [SerializeField] private float waveSpeed = 10f;
    [SerializeField] private float waveDuration = 2f;
    [SerializeField] private Vector2 startPosition = new Vector2(0, -8);
    [SerializeField] private Vector2 endPosition = new Vector2(0, 0);

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void PlayWaveAnimation()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        StopAllCoroutines();
        StartCoroutine(WaveAnimation());
    }

    private IEnumerator WaveAnimation()
    {
        transform.position = startPosition;

        Color color = spriteRenderer.color;
        color.a = 0.6f;
        spriteRenderer.color = color;

        float elapsed = 0f;

        while (elapsed < waveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / waveDuration;

            transform.position = Vector2.Lerp(startPosition, endPosition, t);

            // Wobbling
            float wave = Mathf.Sin(Time.time * 5f) * 0.2f;
            transform.position += new Vector3(0, wave, 0);

            // Fade out
            Color currentColor = spriteRenderer.color;
            currentColor.a = 0.6f * (1f - t);
            spriteRenderer.color = currentColor;

            yield return null;
        }
        gameObject.SetActive(false);
    }
}