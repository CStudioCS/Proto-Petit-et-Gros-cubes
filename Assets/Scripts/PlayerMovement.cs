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
    public float dureeBasculementMin = 0.4f;
    public float dureeBasculementMax = 0.8f;
    public float delaiSansProgres = 0.5f;
    public bool recalerRotation = true;

    public static event System.Action OnRespawn;

    private enum Etat { Repos, Basculement, Stabilisation }
    private Etat etat = Etat.Repos;

    private Rigidbody rb;
    private Vector3 axe;
    private Quaternion rotDepart;
    private float progres;          // degrés parcouru autour de l'axe
    private float meilleurProgres;
    private float tempsSansProgres;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = 20f;
        rb.angularDamping = 0f;
        spawnPosition = rb.position;
        spawnRotation = rb.rotation;
    }

    void Update()
    {
        if (etat != Etat.Repos) return;

        Keyboard k = Keyboard.current;
        if (k == null) return;

        Vector3 dir = Vector3.zero;
        if (k[avancer].isPressed) dir = Vector3.forward;
        else if (k[reculer].isPressed) dir = Vector3.back;
        else if (k[gauche].isPressed) dir = Vector3.left;
        else if (k[droite].isPressed) dir = Vector3.right;

        if (dir == Vector3.zero) return;

        axe = Vector3.Cross(Vector3.up, dir).normalized;
        rotDepart = rb.rotation;
        progres = meilleurProgres = 0f;
        tempsSansProgres = 0f;
        etat = Etat.Basculement;
    }

    void FixedUpdate()
    {
        if (etat == Etat.Basculement) Basculer();
        else if (etat == Etat.Stabilisation) Stabiliser();
    }

    void Basculer()
    {
        float dt = Time.fixedDeltaTime;

        Quaternion delta = rb.rotation * Quaternion.Inverse(rotDepart);
        delta.ToAngleAxis(out float ang, out Vector3 ax);
        if (ang > 180f) ang -= 360f;
        progres = ang * Mathf.Sign(Vector3.Dot(ax, axe)) * (Mathf.Abs(ang) < 0.001f ? 0f : 1f);

        if (progres >= 87f) // petite marge au cas où il monte un escalier par exemple
        {
            Finir();
            return;
        }

        // Abandon si aucune progression
        if (progres > meilleurProgres + 1f)
        {
            meilleurProgres = progres;
            tempsSansProgres = 0f;
        }
        else
        {
            tempsSansProgres += dt;
            if (tempsSansProgres >= delaiSansProgres)
            {
                etat = Etat.Stabilisation;
                return;
            }
        }

        // Moteur : vitesse angulaire visée, réduite en approchant de 90°
        float dureeBasculement = Mathf.Lerp(dureeBasculementMin, dureeBasculementMax, Mathf.Pow(rb.mass, 1/3f) / 5f);
        float omegaNominal = 90f * Mathf.Deg2Rad / dureeBasculement;
        float restantRad = (90f - progres) * Mathf.Deg2Rad;
        float omegaCible = Mathf.Min(omegaNominal, restantRad * 15f);

        float omegaActuel = Vector3.Dot(rb.angularVelocity, axe);
        float accel = (omegaCible - omegaActuel) / dt;

        rb.AddTorque(axe * accel, ForceMode.Acceleration);
    }

    void Finir()
    {
        rb.angularVelocity = Vector3.zero;
        if (recalerRotation) rb.rotation = SnapRotation(rb.rotation);
        etat = Etat.Repos;
    }

    void Stabiliser()
    {
        // On laisse la physique retomber, puis on rend la main
        if (rb.angularVelocity.sqrMagnitude < 0.01f && rb.linearVelocity.sqrMagnitude < 0.01f)
        {
            if (recalerRotation) rb.rotation = SnapRotation(rb.rotation);
            etat = Etat.Repos;
        }
    }

    // Aligne la rotation sur l'orientation à 90° la plus proche
    static Quaternion SnapRotation(Quaternion q)
    {
        Vector3 f = SnapAxe(q * Vector3.forward);
        Vector3 u = SnapAxe(q * Vector3.up);
        if (Mathf.Abs(Vector3.Dot(f, u)) > 0.5f) return q; // sécurité
        return Quaternion.LookRotation(f, u);
    }

    static Vector3 SnapAxe(Vector3 v)
    {
        Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        if (a.x >= a.y && a.x >= a.z) return new Vector3(Mathf.Sign(v.x), 0, 0);
        if (a.y >= a.z) return new Vector3(0, Mathf.Sign(v.y), 0);
        return new Vector3(0, 0, Mathf.Sign(v.z));
    }

    public void respawn()
    {
        etat = Etat.Repos;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = spawnPosition;
        rb.rotation = spawnRotation;
        OnRespawn?.Invoke();
    }
}