using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class WeaponSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI levelText; 
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color filledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    public void SetWeapon(Weapon weapon)
    {
        if (weapon == null)
        {
            // Empty
            iconImage.enabled = false;
            levelText.enabled = false;
            backgroundImage.color = emptyColor;
        }
        else
        {
            // Equipped
            iconImage.enabled = true;
            levelText.enabled = true;
            backgroundImage.color = filledColor;

            if (weapon.data.icon != null)
            {
                iconImage.sprite = weapon.data.icon;
            }

            levelText.text = weapon.level.ToString();
        }
    }
}