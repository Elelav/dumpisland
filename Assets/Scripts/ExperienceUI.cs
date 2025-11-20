using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperienceUI : MonoBehaviour
{
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText;

    void Start()
    {
        if (playerExperience != null)
        {
            playerExperience.OnExpChanged.AddListener(UpdateExpBar);
            playerExperience.OnLevelUp.AddListener(UpdateLevel);
        }
    }

    private void UpdateExpBar(float currentExp, float maxExp)
    {
        expSlider.maxValue = maxExp;
        expSlider.value = currentExp;
    }

    private void UpdateLevel(int level)
    {
        expText.text = $"Level {level}";
        
        StartCoroutine(LevelUpAnimation());
    }

    private System.Collections.IEnumerator LevelUpAnimation()
    {
        Vector3 originalScale = expText.transform.localScale;

        for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            expText.transform.localScale = originalScale * (1f + Mathf.Sin(t * 10f) * 0.5f);
            yield return null;
        }

        expText.transform.localScale = originalScale;
    }
}