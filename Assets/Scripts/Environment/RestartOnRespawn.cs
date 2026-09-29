using UnityEngine;

public class RestartOnRespawn : MonoBehaviour
{
    [Header("Réglages")]
    public bool restartPosition = true;
    public bool restartRotation = true;
    public bool restartLinearVelocity = true;
    public bool restartAngularVelocity = true;


    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
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
        if (restartLinearVelocity)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }

        if (restartAngularVelocity)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.angularVelocity = Vector3.zero;
            }
        }

    }
    
    void OnEnable()
    {
        PlayerMovement.OnRespawn += Restart;
    }
    void OnDisable()
    { 
        PlayerMovement.OnRespawn -= Restart;
    }

}
