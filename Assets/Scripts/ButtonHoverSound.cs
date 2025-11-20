using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private bool playSound = true;
    [SerializeField] private float volume = 0.2f;

    // Called when mouse enters the button
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playSound) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySelectedReward();
        }
    }
}