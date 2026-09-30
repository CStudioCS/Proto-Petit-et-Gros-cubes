using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class physicsBasedPlayerMovement : MonoBehaviour
{

    public Rigidbody rb;

    float targetVelocity;

    float movementX;
    float movementY;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnMove(InputValue movementInput)
    {
        Vector2 movementVector = movementInput.Get<Vector2>();
        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Console.WriteLine(movementX + " " + movementY);
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        rb.AddForce(movement);

        // if (rb.linearVelocity.magnitude < targetVelocity)
        // {
        //     rb.AddForce(movement);
        // }
        // else if (rb.linearVelocity.magnitude > targetVelocity)
        // {
        //     Vector3 clampedVelocity = rb.linearVelocity;

            
        // }
        



    }
}
