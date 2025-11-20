using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;
    private float elapsed = 0f;
    private Vector2 startPosition;
    private Vector2 randomOffset;
    private Canvas canvas;
    private Camera mainCamera;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
    }

    void Start()
    {
        startPosition = rectTransform.anchoredPosition;
        randomOffset = new Vector2(Random.Range(-30f, 30f), 0);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;

        if (t >= 1f)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        float yOffset = moveCurve.Evaluate(t) * moveSpeed;
        rectTransform.anchoredPosition = startPosition + new Vector2(randomOffset.x * t, yOffset);

        if (textMesh != null)
        {
            Color color = textMesh.color;

            if (t > 0.7f)
            {
                color.a = 1f - ((t - 0.7f) / 0.3f);
            }
            else
            {
                color.a = 1f;
            }

            textMesh.color = color;
        }
    }

    public void Initialize(float damage, bool isCrit, Vector3 worldPosition)
    {
        // Get components if not yet available
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        // Fix: Get canvas here, not in Awake
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        // If still null - search in scene
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            DebugHelper.LogWarning("Canvas not found via GetComponentInParent, using FindObjectOfType");
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Check that everything is initialized
        if (canvas == null)
        {
            DebugHelper.LogError("Canvas still null! Damage number cannot be shown.");
            Destroy(gameObject);
            return;
        }

        // Convert world position to screen position
        Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPosition);

        // Convert screen to Canvas position
        RectTransform canvasRect = canvas.transform as RectTransform;

        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out canvasPos
        );

        rectTransform.anchoredPosition = canvasPos;
        startPosition = canvasPos;

        // Configure text
        if (textMesh != null)
        {
            int damageInt = Mathf.RoundToInt(damage);
            textMesh.text = damageInt.ToString();

            if (isCrit)
            {
                textMesh.fontSize = 48;
                textMesh.color = Color.yellow;
                textMesh.fontStyle = FontStyles.Bold;
                textMesh.text = damageInt.ToString() + "!";
            }
            else
            {
                textMesh.fontSize = 32;
                textMesh.fontStyle = FontStyles.Normal;

                if (damage >= 50)
                {
                    textMesh.color = new Color(1f, 0.6f, 0f);
                }
                else if (damage >= 20)
                {
                    textMesh.color = Color.white;
                }
                else
                {
                    textMesh.color = new Color(0.7f, 0.7f, 0.7f);
                }
            }
        }

        gameObject.SetActive(true);
        elapsed = 0f;
    }
}