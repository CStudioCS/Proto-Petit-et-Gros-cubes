using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Joueurs à filmer")]
    public Transform J1;
    public Transform J2;

    [Header("Paramètres")]
    public Vector3 Offset = new Vector3(0, 15, -10);

    void Update()
    {
        transform.position = new Vector3(
            (J1.position.x + J2.position.x)/2,
            0f,
            (J1.position.z + J2.position.z)/2
        ) + Offset;
    }
}