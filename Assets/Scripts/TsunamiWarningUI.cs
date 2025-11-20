using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TsunamiWarningUI : MonoBehaviour
{
    public static TsunamiWarningUI Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private Image warningOverlay;
    [SerializeField] private Color warningColor = new Color(1f, 0.2f, 0.2f, 0.3f);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Hide by default
        if (warningOverlay != null)
        {
            warningOverlay.color = new Color(warningColor.r, warningColor.g, warningColor.b, 0);
        }
    }

    /// <summary>
    /// Show tsunami warning
    /// </summary>
    public void ShowWarning(float duration)
    {
        if (warningOverlay != null)
        {
            StartCoroutine(WarningFlash(duration));
        }
    }

    private IEnumerator WarningFlash(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Pulsing (fast at start, slow at end)
            float pulseSpeed = Mathf.Lerp(8f, 3f, elapsed / duration);
            float alpha = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f; // 0-1
            alpha *= warningColor.a; // Max alpha

            // Fade out at the end
            alpha *= Mathf.Lerp(1f, 0f, Mathf.Clamp01((elapsed - duration * 0.7f) / (duration * 0.3f)));

            warningOverlay.color = new Color(warningColor.r, warningColor.g, warningColor.b, alpha);

            yield return null;
        }

        // Fully hide
        warningOverlay.color = new Color(warningColor.r, warningColor.g, warningColor.b, 0);
    }
}