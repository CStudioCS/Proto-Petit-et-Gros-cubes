using UnityEngine;

public class RestartOnRespawn : MonoBehaviour
{
    [Header("Réglages")]
    public bool restartPosition = true;
    public bool restartRotation = true;
    public bool restartRigidbodyParameters = false;

    private Rigidbody rb;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // État initial du Rigidbody
    private float initialMass;
    private float initialLinearDamping;
    private float initialAngularDamping;
    private bool initialUseGravity;
    private bool initialIsKinematic;
    private RigidbodyInterpolation initialInterpolation;
    private CollisionDetectionMode initialCollisionDetection;
    private RigidbodyConstraints initialConstraints;
    private bool initialDetectCollisions;

    // État physique au moment du démarrage
    private Vector3 initialVelocity;
    private Vector3 initialAngularVelocity;

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            initialMass = rb.mass;
            initialLinearDamping = rb.linearDamping;
            initialAngularDamping = rb.angularDamping;
            initialUseGravity = rb.useGravity;
            initialIsKinematic = rb.isKinematic;
            initialInterpolation = rb.interpolation;
            initialCollisionDetection = rb.collisionDetectionMode;
            initialConstraints = rb.constraints;
            initialDetectCollisions = rb.detectCollisions;

            initialVelocity = rb.linearVelocity;
            initialAngularVelocity = rb.angularVelocity;
        }
    }

    public void Restart()
    {
        if (restartPosition)
        {
            transform.position = initialPosition;
        }

        if (restartRotation)
        {
            transform.rotation = initialRotation;
        }

        if (rb != null && restartRigidbodyParameters)
        {
            rb.mass = initialMass;
            rb.linearDamping = initialLinearDamping;
            rb.angularDamping = initialAngularDamping;
            rb.useGravity = initialUseGravity;
            rb.isKinematic = initialIsKinematic;
            rb.interpolation = initialInterpolation;
            rb.collisionDetectionMode = initialCollisionDetection;
            rb.constraints = initialConstraints;
            rb.detectCollisions = initialDetectCollisions;
            rb.linearVelocity = initialVelocity;
            rb.angularVelocity = initialAngularVelocity;

            rb.WakeUp();
        }
        else
        {
            Destroy(GetComponent<Rigidbody>());
        }
    }

    private void OnEnable()
    {
        PlayerMovement.OnRespawn += Restart;
    }

    private void OnDisable()
    {
        PlayerMovement.OnRespawn -= Restart;
    }
}