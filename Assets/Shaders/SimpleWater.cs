using UnityEngine;

public class SimpleWater : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private Material waterMaterial;
    [SerializeField] private Vector2 areaSize = new Vector2(100f, 100f);

    [Header("Цвета (опционально, можно настраивать в материале)")]
    [SerializeField] private Color deepColor = new Color(0.1f, 0.4f, 0.7f, 1f);
    [SerializeField] private Color shallowColor = new Color(0.3f, 0.6f, 0.9f, 1f);
    [SerializeField] private Color foamColor = new Color(0.8f, 0.9f, 1f, 1f);

    [Header("Параметры волн")]
    [SerializeField] private float waveSpeed = 0.5f;
    [SerializeField] private float waveScale = 5f;
    [SerializeField] private float foamThreshold = 0.7f;

    void Start()
    {
        CreateWaterPlane();
    }

    [ContextMenu("Создать воду")]
    void CreateWaterPlane()
    {
        // Удаляем старую воду
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Создаём один большой quad с шейдером
        GameObject waterPlane = new GameObject("WaterPlane");
        waterPlane.transform.SetParent(transform);
        waterPlane.transform.localPosition = Vector3.zero;

        // Добавляем SpriteRenderer
        SpriteRenderer sr = waterPlane.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Background";
        sr.sortingOrder = -10;

        // Создаём простой белый спрайт
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sr.sprite = sprite;

        // Масштабируем под нужный размер
        waterPlane.transform.localScale = new Vector3(areaSize.x, areaSize.y, 1f);

        // Применяем материал с шейдером
        if (waterMaterial != null)
        {
            sr.material = waterMaterial;

            // Устанавливаем параметры
            sr.material.SetColor("_DeepColor", deepColor);
            sr.material.SetColor("_ShallowColor", shallowColor);
            sr.material.SetColor("_FoamColor", foamColor);
            sr.material.SetFloat("_WaveSpeed", waveSpeed);
            sr.material.SetFloat("_WaveScale", waveScale);
            sr.material.SetFloat("_FoamThreshold", foamThreshold);
        }
        else
        {
            DebugHelper.LogWarning("Материал воды не назначен!");
        }

        DebugHelper.Log("🌊 Создана shader-вода!");
    }
}