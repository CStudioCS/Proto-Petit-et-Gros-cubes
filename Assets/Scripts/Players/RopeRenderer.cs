using UnityEngine;

public class RopeLine : MonoBehaviour
{
    [SerializeField] private LineRenderer line;
    [SerializeField] private Transform[] points;

    void LateUpdate()
    {
        line.positionCount = points.Length;

        for (int i = 0; i < points.Length; i++)
        {
            line.SetPosition(i, points[i].position);
        }
    }
}