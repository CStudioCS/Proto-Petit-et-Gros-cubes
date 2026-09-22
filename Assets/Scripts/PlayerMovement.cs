using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Contrôles")]
    public Key avancer = Key.Z;
    public Key reculer = Key.S;
    public Key gauche = Key.Q;
    public Key droite = Key.D;

    [Header("Réglages")]
    public float animationTime = 1.75f;
    public Animator animator;

    public static event System.Action OnRespawn;

    private Rigidbody rb;
    private Vector3? cible = null;
    private float tempsRestantAvantAbandon;
    private float vitesse;

    private Vector3 direction = Vector3.zero;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        spawnPosition = rb.position;
        spawnRotation = rb.rotation;
    }

    void Update()
    {
        if (cible != null)
            return;

        Keyboard k = Keyboard.current;
        if (k == null) return;

        direction = Vector3.zero;

        if (k[avancer].isPressed) direction = Vector3.forward;
        else if (k[reculer].isPressed) direction = Vector3.back;
        else if (k[gauche].isPressed) direction = Vector3.left;
        else if (k[droite].isPressed) direction = Vector3.right;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            float taille = transform.localScale.x;
            vitesse = transform.localScale.x / animationTime;

            cible = rb.position + direction * taille;

            tempsRestantAvantAbandon = (taille / vitesse) * 1.1f; // * sécu
            
            animator.SetTrigger("Roll");
        }
    }

    void FixedUpdate()
    {
        if (cible == null || direction == Vector3.zero)
            return;
        
        rb.MoveRotation(Quaternion.LookRotation(direction, Vector3.up));

        Vector3 position = rb.position;

        Vector3 prochainePos = Vector3.MoveTowards(
            new Vector3(position.x, 0f, position.z),
            new Vector3(cible.Value.x, 0f, cible.Value.z),
            vitesse * Time.fixedDeltaTime
        );

        rb.MovePosition(new Vector3(prochainePos.x, position.y, prochainePos.z));

        float distance = Vector3.Distance(
            new Vector3(rb.position.x, 0f, rb.position.z),
            new Vector3(cible.Value.x, 0f, cible.Value.z)
        );

        tempsRestantAvantAbandon -= Time.fixedDeltaTime;

        // si on est bloqué trop longtemps par un obstacle (ou qu'on est arrivé) on libère cible
        if (distance <= 0 || tempsRestantAvantAbandon <= 0f)
        {
            cible = null;
        }
    }

    public void respawn()
    {
        cible = null;
        direction = Vector3.zero;
        vitesse = 0f;

        rb.position = spawnPosition;
        rb.rotation = spawnRotation;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        animator.Rebind();
        animator.Update(0f);
        
        OnRespawn?.Invoke();
    }
}