using UnityEngine;
using UnityEngine.UI;

public class DropOffProgressUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image progressCircle;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private Color progressColor = Color.green;
    [SerializeField] private float fadeSpeed = 5f;

    private bool isVisible = false;

    void Awake()
    {
        DebugHelper.Log($"[ProgressUI] Awake - ProgressCircle: {(progressCircle != null ? "OK" : "NULL")}, CanvasGroup: {(canvasGroup != null ? "OK" : "NULL")}");

        if (progressCircle != null)
        {
            progressCircle.fillAmount = 0f;
            progressCircle.color = progressColor;
            progressCircle.type = Image.Type.Filled;
            progressCircle.fillMethod = Image.FillMethod.Radial360;
            progressCircle.fillOrigin = (int)Image.Origin360.Top;

            DebugHelper.Log($"[ProgressUI] ProgressCircle configured: fillAmount={progressCircle.fillAmount}, color={progressCircle.color}");
        }
        else
        {
            DebugHelper.LogError("[ProgressUI] ProgressCircle = NULL! Assign Image in Inspector!");
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            DebugHelper.Log($"[ProgressUI] CanvasGroup alpha = {canvasGroup.alpha}");
        }
        else
        {
            DebugHelper.LogError("[ProgressUI] CanvasGroup = NULL! Add Canvas Group component!");
        }
    }

    void Update()
    {
        if (canvasGroup != null)
        {
            float targetAlpha = isVisible ? 1f : 0f;
            float oldAlpha = canvasGroup.alpha;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

            // Log only when changing
            if (Mathf.Abs(oldAlpha - canvasGroup.alpha) > 0.01f)
            {
                DebugHelper.Log($"[ProgressUI] Alpha: {canvasGroup.alpha:F2} (target: {targetAlpha}, visible: {isVisible})");
            }
        }
    }

    public void Show()
    {
        DebugHelper.Log("[ProgressUI] ===== SHOW CALLED =====");
        isVisible = true;
        if (progressCircle != null)
        {
            progressCircle.fillAmount = 0f;
        }
    }

    public void Hide()
    {
        DebugHelper.Log("[ProgressUI] ===== HIDE CALLED =====");
        isVisible = false;
    }

    public void SetProgress(float progress)
    {
        if (progressCircle != null)
        {
            progressCircle.fillAmount = Mathf.Clamp01(progress);
            DebugHelper.Log($"[ProgressUI] Progress: {progress:F2}, FillAmount: {progressCircle.fillAmount:F2}");
        }
    }
}