using UnityEngine;

public class FullBagIndicator : MonoBehaviour
{
    [SerializeField] private GarbageBag garbageBag;
    [SerializeField] private GameObject warningIcon;

    void Start()
    {
        if (warningIcon != null)
        {
            warningIcon.SetActive(false);
        }

        if (garbageBag != null)
        {
            garbageBag.OnCapacityChanged.AddListener(CheckBagStatus);
        }
    }

    private void CheckBagStatus(int current, int max)
    {
        if (warningIcon != null)
        {
            warningIcon.SetActive(current >= max);
        }
    }
}