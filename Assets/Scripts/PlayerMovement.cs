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

    private Rigidbody rb;
    private Vector3? cible = null;
    private float tempsRestantAvantAbandon;
    private float vitesse;

    private Vector3 direction = Vector3.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (cible != null)
            return;

        Keyboard k = Keyboard.current;
        if (k == null) return;

        direction = Vector3.zero;

        if (k[avancer].wasPressedThisFrame) direction = Vector3.forward;
        else if (k[reculer].wasPressedThisFrame) direction = Vector3.back;
        else if (k[gauche].wasPressedThisFrame) direction = Vector3.left;
        else if (k[droite].wasPressedThisFrame) direction = Vector3.right;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            float taille = transform.localScale.x;
            vitesse = transform.localScale.x / animationTime;

            cible = rb.position + direction * taille;

            // Temps théorique du trajet + marge de sécurité
            // en gros si ce délai est dépassé (obstacle), on abandonne la cible
            tempsRestantAvantAbandon = (taille / vitesse) * 1.1f;
            
            animator.SetTrigger("Roll");
        }
    }

    void FixedUpdate()
    {
        if (cible == null || direction == Vector3.zero)
            return;
        
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

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

        // Arrivé, OU bloqué trop longtemps par un obstacle -> on libère cible
        if (distance <= 0 || tempsRestantAvantAbandon <= 0f)
        {
            cible = null;
        }
    }
}