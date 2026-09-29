using UnityEngine;

public class TwoPlayerCamera : MonoBehaviour
{
    [Header("Cibles")]
    public Transform J1;
    public Transform J2;

    [Header("Caméra")]
    public float cameraHeight = 15f;
    public float rotationX = 35f;
    public float minDistance = 10f;
    public float distanceMultiplier = 1.2f;


    [Header("Mouvement")]
    public float smoothSpeed = 5f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (J1 == null || J2 == null)
            return;

        Vector3 center = (J1.position + J2.position) / 2f;

        float playerDistance = Vector3.Distance(J1.position, J2.position);

        float targetDistance = Mathf.Max(
            minDistance,
            playerDistance * distanceMultiplier
        );

        Quaternion rotation = Quaternion.Euler(rotationX, 0f, 0f);
        Vector3 backward = -(rotation * Vector3.forward);

        Vector3 targetPosition = center + backward * targetDistance;
        targetPosition.y = center.y + cameraHeight;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.rotation = rotation;
    }
}
