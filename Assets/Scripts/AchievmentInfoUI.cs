using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AchievmentInfoUI : MonoBehaviour 
{
    [Header("UI elements")]
    [SerializeField] private Image achievmentIcon;
    [SerializeField] private TextMeshProUGUI achievmentText;

    public void SetupAchievment(Achievment achiemventData) 
    {
        if (achievmentIcon == null)
        { 
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (achiemventData != null && achievmentIcon != null)
        {
            achievmentIcon.sprite = achiemventData.icon;            
        }

        if (achiemventData != null && achievmentText != null)
        {
            achievmentText.text = achiemventData.description;
        }        

    }

    public void setPanelActive(bool active = true) 
    {
        gameObject.SetActive(active);
    }
}
