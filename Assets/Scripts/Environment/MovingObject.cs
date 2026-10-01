using UnityEngine;

public class MovingObject : MonoBehaviour
{
    [Header("Translation")]
    public bool isMoving = false;
    public float speed = 0f;
    public Vector3[] targets;

    [Header("Rotation")]
    public bool isRotating = false;
    public Vector3 angularSpeed; //en °/s 
    private int targetIndice;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        targetIndice = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {
            float distance = speed * Time.deltaTime;
            while (distance > (targets[targetIndice] - transform.position).magnitude)
            {
                distance -= (targets[targetIndice] - transform.position).magnitude;
                transform.position = targets[targetIndice];
                targetIndice = (targetIndice + 1) % targets.Length;
            }
            Vector3 direction = targets[targetIndice] - transform.position;

            if (direction != Vector3.zero)
            {
                transform.position += distance * direction.normalized;
            }
        }
        if(isRotating)
        {
            float magnitude = angularSpeed.magnitude*Time.deltaTime;
            Vector3 axis = angularSpeed.normalized;
            transform.rotation *= Quaternion.AngleAxis(magnitude,axis);
        }
    }
}