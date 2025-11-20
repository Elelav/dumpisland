using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GarbageBagUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GarbageBag garbageBag;
    [SerializeField] private Slider fillSlider;
    [SerializeField] private TextMeshProUGUI bagText;
    [SerializeField] private Image fillImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.8f, 0.6f, 0.2f);
    [SerializeField] private Color fullColor = Color.red;

    // New: Threshold to determine “unlimited” bag
    [SerializeField] private int unlimitedThreshold = 100000;

    void Start()
    {
        if (garbageBag != null)
        {
            garbageBag.OnCapacityChanged.AddListener(UpdateUI);
            UpdateUI(0, garbageBag.GetMaxCapacity());
        }
    }

    private void UpdateUI(int current, int max)
    {
        // New: Check if bag is unlimited
        bool isUnlimited = max >= unlimitedThreshold;

        if (isUnlimited)
        {
            // Unlimited bag
            fillSlider.value = 0f; // or hide the slider entirely
            bagText.text = $"{current}"; // Only current amount

            // Color always normal (can’t be full)
            if (fillImage != null)
            {
                fillImage.color = normalColor;
            }
        }
        else
        {
            // Regular limited bag
            fillSlider.value = max > 0 ? (float)current / max : 0f;
            bagText.text = $"{current} / {max}";

            // Change color if bag is full
            if (fillImage != null)
            {
                fillImage.color = (current >= max) ? fullColor : normalColor;
            }
        }

        // Shake animation when filled
        if (current > 0)
        {
            StartCoroutine(BagShake());
        }
    }

    private System.Collections.IEnumerator BagShake()
    {
        Vector3 originalPosition = transform.localPosition;

        for (int i = 0; i < 5; i++)
        {
            transform.localPosition = originalPosition + (Vector3)Random.insideUnitCircle * 2f;
            yield return new WaitForSeconds(0.02f);
        }

        transform.localPosition = originalPosition;
    }
}