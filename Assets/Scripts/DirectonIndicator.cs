using UnityEngine;
using UnityEngine.UI;

public class DirectionIndicator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private DropOffManager dropOffManager;

    [Header("Visual")]
    [SerializeField] private Image arrowImage;
    [SerializeField] private float hideDistance = 5f; // Distance at which to hide arrow
    [SerializeField] private float distanceFromCenter = 150f; // Distance from screen center

    private RectTransform arrowRect;

    void Start()
    {
        arrowRect = GetComponent<RectTransform>();

        if (arrowImage == null)
        {
            arrowImage = GetComponent<Image>();
        }
    }

    void Update()
    {
        if (player == null || dropOffManager == null)
        {
            DebugHelper.LogWarning("DirectionIndicator: Player or DropOffManager not assigned!");
            arrowImage.enabled = false;
            return;
        }

        DropOffPoint activePoint = dropOffManager.GetActivePoint();

        if (activePoint == null || !activePoint.IsActive())
        {
            arrowImage.enabled = false;
            return;
        }

        Vector3 targetPosition = activePoint.transform.position;
        float distance = Vector2.Distance(player.position, targetPosition);

        // Hide arrow if player is close to target
        if (distance < hideDistance)
        {
            arrowImage.enabled = false;
            return;
        }

        arrowImage.enabled = true;

        // Direction to target (in world coordinates)
        Vector2 direction = (targetPosition - player.position).normalized;

        // Arrow rotation angle
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;// - 90f;
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);

        // Arrow position - simply in direction of target from screen center
        Vector2 arrowPosition = direction * distanceFromCenter;
        arrowRect.anchoredPosition = arrowPosition;

        // Pulsing
        float pulse = 2f + Mathf.Sin(Time.time * 3f) * 0.2f;
        arrowRect.localScale = Vector3.one * pulse;
    }
}