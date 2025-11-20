using UnityEngine;
using System.Collections.Generic;

public class DropOffManager : MonoBehaviour
{
    [Header("Drop-off Points")]
    [SerializeField] private List<DropOffPoint> dropOffPoints = new List<DropOffPoint>();

    [Header("Settings")]
    [SerializeField] private bool changePointOnWave = true;
    [SerializeField] private bool changePointOnEmpty = false;

    [Header("Dependencies")]
    [SerializeField] private WaveManager waveManager;

    private DropOffPoint activePoint;

    void Start()
    {
        // Fixed: Check that all points are valid
        dropOffPoints.RemoveAll(point => point == null);

        if (dropOffPoints.Count == 0)
        {
            DebugHelper.LogError("DropOffManager: No drop-off points!");
            return;
        }

        // Deactivate all points
        foreach (var point in dropOffPoints)
        {
            if (point != null)
            {
                point.SetActive(false);
            }
        }

        // Activate random one
        ActivateRandomPoint();

        // Subscribe to wave events
        if (waveManager != null && changePointOnWave)
        {
            waveManager.OnWaveStart.AddListener(OnWaveStart);
        }
    }

    private void OnWaveStart(int waveNumber)
    {
        DebugHelper.Log($"Changing active drop-off point for wave {waveNumber}");
        ActivateRandomPoint();
    }

    public void ActivateRandomPoint()
    {
        if (dropOffPoints.Count == 0)
        {
            DebugHelper.LogWarning("No drop-off points!");
            return;
        }

        // Deactivate current
        if (activePoint != null)
        {
            activePoint.SetActive(false);
        }

        // Choose random (but not the same one, if possible)
        DropOffPoint newPoint;
        int attempts = 0;

        do
        {
            int randomIndex = Random.Range(0, dropOffPoints.Count);
            newPoint = dropOffPoints[randomIndex];
            attempts++;

            if (dropOffPoints.Count > 1 && newPoint == activePoint && attempts < 10)
            {
                continue;
            }

            break;

        } while (attempts < 10);

        activePoint = newPoint;

        if (activePoint != null)
        {
            activePoint.SetActive(true);
            DebugHelper.Log($"Activated drop-off point: {activePoint.gameObject.name}");
        }
        else
        {
            DebugHelper.LogError("activePoint = null after selection!");
        }
    }

    public void OnGarbageDroppedOff()
    {
        if (changePointOnEmpty)
        {
            DebugHelper.Log("Garbage dropped off - changing active point");
            ActivateRandomPoint();
        }
    }

    public DropOffPoint GetActivePoint() => activePoint;
}