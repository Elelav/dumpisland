using UnityEngine;

public class AttackVisual : MonoBehaviour
{
    public static void DrawAttackLine(Vector3 from, Vector3 to, Color color, float duration = 0.15f)
    {
        GameObject lineObj = new GameObject("AttackLine");
        LineRenderer line = lineObj.AddComponent<LineRenderer>();

        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);

        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;

        line.sortingOrder = 10;

        Destroy(lineObj, duration);
    }
}