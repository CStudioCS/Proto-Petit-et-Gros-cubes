using UnityEngine;

public class Bumper : MonoBehaviour
{
    [Header("Réglages")]
    public float forceSaut = 12f;

    void OnTriggerEnter(Collider autre)
    {
        if (!autre.CompareTag("Player")) return;

        Rigidbody rb = autre.attachedRigidbody;

        if (rb == null || rb.isKinematic) return;

        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        rb.AddForce(Vector3.up * forceSaut, ForceMode.VelocityChange);
    }
}