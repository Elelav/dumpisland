using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    [Header("Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private bool autoDetectBounds = true; // New
    [SerializeField] private Tilemap boundsTilemap; // New - assign Tilemap

    [SerializeField] private float minX = -18f;
    [SerializeField] private float maxX = 18f;
    [SerializeField] private float minY = -13f;
    [SerializeField] private float maxY = 13f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Automatically calculate bounds from Tilemap
        if (autoDetectBounds && boundsTilemap != null)
        {
            CalculateBoundsFromTilemap();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        // Limit camera position
        if (useBounds)
        {
            // Account for camera size
            float camHeight = cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;

            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX + camWidth, maxX - camWidth);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY + camHeight, maxY - camHeight);
        }

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }

    private void CalculateBoundsFromTilemap()
    {
        boundsTilemap.CompressBounds();
        Bounds bounds = boundsTilemap.localBounds;

        minX = bounds.min.x;
        maxX = bounds.max.x;
        minY = bounds.min.y;
        maxY = bounds.max.y;

        DebugHelper.Log($"Camera bounds automatically set: X({minX} to {maxX}), Y({minY} to {maxY})");
    }

    // For manual bounds update in editor
    [ContextMenu("Update Bounds from Tilemap")]
    private void UpdateBoundsInEditor()
    {
        if (boundsTilemap != null)
        {
            CalculateBoundsFromTilemap();
        }
        else
        {
            DebugHelper.LogWarning("Tilemap not assigned for auto-detection of bounds!");
        }
    }
}