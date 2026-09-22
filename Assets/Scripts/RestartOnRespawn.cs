using UnityEngine;

public class RestartOnRespawn : MonoBehaviour
{
    [Header("Réglages")]
    public bool restartPosition = true;
    public bool restartRotation = true;


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
