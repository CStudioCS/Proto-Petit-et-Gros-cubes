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
    public float vitesseMax = 5f;

    [Header("Détection")]
    [Tooltip("Layers considérés comme des obstacles")]
    public LayerMask solides = ~0;
    [Range(0.5f, 1f)] public float facteurBoite = 0.8f;
    public float seuilChute = 0.002f;

    [Header("Visuel")]
    public Transform visuel;
    public float vitesseRecalage = 360f;

    public static event System.Action OnRespawn;

    private Rigidbody rb;
    private Vector3? cible = null;
    private float tempsRestantAvantAbandon;

    private Vector3 direction = Vector3.zero;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private float vitesse;

    private bool enChute;
    private float dernierY;

    private readonly Collider[] bufferColliders = new Collider[16];
    private readonly RaycastHit[] bufferHits = new RaycastHit[16];

    private Quaternion baseRot;
    private Quaternion correction;
    private float theta;          
    private float residuHauteur; 
    private Vector3 axeRoulis = Vector3.right;
    private Vector3 axeSuivant;
    private bool nouveauRoulement;
    private bool enRoulement;
    private Vector3 dernierePosVisuel;
    private Quaternion baseRotSpawn;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        spawnPosition = rb.position;
        spawnRotation = rb.rotation;
        dernierY = rb.position.y;

        baseRot = ArrondirA90(visuel.rotation);
        correction = visuel.rotation * Quaternion.Inverse(baseRot);
        baseRotSpawn = baseRot;

        dernierePosVisuel = transform.position;
    }

    void Update()
    {
        if (cible != null || enChute)
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

            Vector3 depart = rb.position;
            Vector3 c = depart + direction * taille;

            if (ObstacleEn(c, taille) || ObstacleSurTrajet(depart, direction, taille))
            {
                direction = Vector3.zero;
                return;
            }

            vitesse = vitesseMax / taille;

            cible = c;

            tempsRestantAvantAbandon = (taille / vitesse) * 1.1f; // * sécu

            axeSuivant = Vector3.Cross(Vector3.up, direction);
            nouveauRoulement = true;
        }
    }




    void FixedUpdate()
    {
        enChute = rb.position.y < dernierY - seuilChute;
        dernierY = rb.position.y;

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

        if (distance <= 0 || tempsRestantAvantAbandon <= 0f)
        {
            cible = null;
        }
    }




    private bool EstSoi(Collider c)
    {
        return c.transform == transform || c.transform.IsChildOf(transform);
    }

    private Vector3 DemiTaille(float taille)
    {
        return Vector3.one * (taille * 0.5f * facteurBoite);
    }

    // Le cube, placé à cette position, serait-il dans un obstacle ?
    private bool ObstacleEn(Vector3 centre, float taille)
    {
        int n = Physics.OverlapBoxNonAlloc(centre, DemiTaille(taille), bufferColliders,
            Quaternion.identity, solides, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
            if (!EstSoi(bufferColliders[i])) return true;

        return false;
    }




    private bool ObstacleSurTrajet(Vector3 depart, Vector3 dir, float taille)
    {
        int n = Physics.BoxCastNonAlloc(depart, DemiTaille(taille), dir, bufferHits,
            Quaternion.identity, taille, solides, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
            if (!EstSoi(bufferHits[i].collider)) return true;

        return false;
    }




    void LateUpdate()
    {
        float taille = transform.localScale.x;
        Vector3 pos = transform.position;

        Vector3 delta = pos - dernierePosVisuel;
        delta.y = 0f;
        dernierePosVisuel = pos;

        if (enRoulement)
            theta = Mathf.Min(theta + (delta.magnitude / taille) * 90f, 90f);

        if (enRoulement && (nouveauRoulement || (cible == null && delta.sqrMagnitude < 1e-8f)))
        {
            Rebaser(taille);
            enRoulement = false;
        }

        if (nouveauRoulement)
        {
            axeRoulis = axeSuivant;
            enRoulement = true;
            nouveauRoulement = false;
        }

        correction = Quaternion.RotateTowards(correction, Quaternion.identity, vitesseRecalage * Time.deltaTime);
        residuHauteur = Mathf.MoveTowards(residuHauteur, 0f, taille * (vitesseRecalage / 90f) * Time.deltaTime);

        visuel.rotation = Quaternion.AngleAxis(theta, axeRoulis) * correction * baseRot;
        visuel.position = pos + Vector3.up * (residuHauteur + DecalageHauteur(theta, taille));
    }

    private static float DecalageHauteur(float thetaDeg, float a)
    {
        float t = thetaDeg * Mathf.Deg2Rad;
        return a / Mathf.Sqrt(2f) * Mathf.Sin(t + Mathf.PI / 4f) - a / 2f; // formule de la hauteur d'un carré qui roule sur un plan
    }

    private void Rebaser(float taille)
    {
        Quaternion actuelle = Quaternion.AngleAxis(theta, axeRoulis) * correction * baseRot;

        residuHauteur += DecalageHauteur(theta, taille);
        theta = 0f;

        baseRot = ArrondirA90(actuelle);
        correction = actuelle * Quaternion.Inverse(baseRot);
    }

    private static Quaternion ArrondirA90(Quaternion q)
    {
        Vector3 f = AxeLePlusProche(q * Vector3.forward);
        Vector3 u = q * Vector3.up;
        u -= f * Vector3.Dot(u, f); // up doit être perpendiculaire à forward
        return Quaternion.LookRotation(f, AxeLePlusProche(u));
    }

    private static Vector3 AxeLePlusProche(Vector3 v)
    {
        float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);

        if (ax >= ay && ax >= az) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
        if (ay >= az) return new Vector3(0f, Mathf.Sign(v.y), 0f);
        return new Vector3(0f, 0f, Mathf.Sign(v.z));
    }

    public void respawn()
    {
        cible = null;
        direction = Vector3.zero;
        vitesse = 0f;
        enChute = false;

        rb.position = spawnPosition;
        rb.rotation = spawnRotation;
        dernierY = spawnPosition.y;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        enRoulement = false;
        nouveauRoulement = false;
        theta = 0f;
        residuHauteur = 0f;
        baseRot = baseRotSpawn;
        correction = Quaternion.identity;
        dernierePosVisuel = spawnPosition;

        visuel.rotation = baseRot;
        visuel.position = spawnPosition;

        OnRespawn?.Invoke();
    }
}