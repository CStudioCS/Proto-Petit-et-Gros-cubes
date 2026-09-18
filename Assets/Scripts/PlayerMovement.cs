using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Contrôles")]
    public Key avancer = Key.Z;
    public Key reculer = Key.S;
    public Key gauche = Key.Q;
    public Key droite = Key.D;

    [Header("Réglages")]
    public float vitesse = 4f;
    public float vitesseRotation = 10f; // pour orienter visuellement le cube

    private Rigidbody rb;

    void FixedUpdate()
    {
        var k = Keyboard.current;
        Vector3 direction = Vector3.zero;

        if (k[avancer].isPressed) direction += Vector3.forward;
        if (k[reculer].isPressed) direction += Vector3.back;
        if (k[gauche].isPressed)  direction += Vector3.left;
        if (k[droite].isPressed)  direction += Vector3.right;

        direction = direction.normalized;

        // On fixe la vitesse horizontale, la gravité verticale n'est pas touchée
        Vector3 v = direction * vitesse;
        v.y = rb.linearVelocity.y; // "linearVelocity" en Unity 6 (sinon rb.velocity)
        rb.linearVelocity = v;

        // Rotation visuelle vers la direction (facultatif, remplace ton animation de roulement)
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion cible = Quaternion.LookRotation(direction, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, cible, vitesseRotation * Time.fixedDeltaTime));
        }
    }
}