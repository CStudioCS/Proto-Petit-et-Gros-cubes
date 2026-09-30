using UnityEngine;
using UnityEngine.InputSystem;

/// Explication vite fait :
/// - Le collider avance en ligne droite en glissant, sans jamais rouler.
/// - Le visuel est séparé : il roule de 90° à chaque déplacement et suit le collider si la physique le fait basculer (bord du vide).
 
 
public class PlayerMovement : MonoBehaviour
{
    [Header("Contrôles")]
    public Key avancer = Key.Z;
    public Key reculer = Key.S;
    public Key gauche = Key.Q;
    public Key droite = Key.D;

    [Header("Réglages")]
    public float vitesseMin = 5f;  // vitesse du plus gros cube
    public float vitesseMax = 8f;   // vitesse du plus petit cube

    [Header("Détection")]
    public LayerMask solides = ~0;
    [Range(0.5f, 1f)] public float facteurBoite = 0.8f;
    [Range(0f, 0.9f)] public float hauteurMontable = 0.3f;
    public float seuilChute = 0.002f;

    [Header("Corde")]
    public RopeController corde;
    [Range(0f, 1f)] public float tensionMax = 0.9f; 
    public bool isOnFirstBone;

    [Header("Visuel")]
    public Transform visuel;
    public float vitesseRecalage = 360f;  

    public static event System.Action OnRespawn;

    //  Physique
    private Rigidbody rb;
    private Vector3 direction;
    private Vector3? cible;
    private float vitesse;
    private float tempsAvantAbandon;
    private bool enChute;
    private float dernierY;
    private bool rouleSurPlace;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private readonly RaycastHit[] bufferHits = new RaycastHit[16];

    // Animation du visuel
    private Quaternion baseRot;
    private Quaternion correction;
    private Quaternion rotRef;
    private Quaternion baseRotSpawn;
    private float theta;
    private Vector3 axeRoulis = Vector3.right;
    private bool enRoulement;
    private float residuHauteur;
    private Vector3 dernierePos;

    private float Taille => transform.localScale.x;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        spawnPosition = rb.position;
        spawnRotation = rb.rotation;
        dernierY = rb.position.y;
        dernierePos = transform.position;

        rotRef = transform.rotation;
        baseRot = ArrondirA90(visuel.rotation, transform.rotation);
        correction = visuel.rotation * Quaternion.Inverse(baseRot);
        baseRotSpawn = baseRot;
    }

    void Update()
    {
        // Pas de nouvel ordre tant que le cube bouge, tombe ou roule sur place
        if (cible != null || enChute || rouleSurPlace) return;

        Vector3 dir = LireDirection();
        if (dir != Vector3.zero)
            DemarrerDeplacement(dir);
    }

    /////////////////////////////////////////////////////// Partie mouvement ///////////////////////////////////////////////////////

    void FixedUpdate()
    {
        enChute = rb.position.y < dernierY - seuilChute;
        dernierY = rb.position.y;

        if (cible == null) return;

        // Déplacement horizontal uniquement (Y géré par la physique)
        Vector3 pos = rb.position;
        Vector3 cibleXZ = new Vector3(cible.Value.x, 0f, cible.Value.z);
        Vector3 suivant = Vector3.MoveTowards(new Vector3(pos.x, 0f, pos.z), cibleXZ, vitesse * Time.fixedDeltaTime);

        // sécurité pour ne pas s'éloigner de plus de RopeRange du coequipier s'il bouge en même temps
        if (CordeBloque(direction))
        {
            cible = null;           // on s'arrête là
            rouleSurPlace = true;   // l'animation de roulement se termine sur place
            return;
        }


        rb.MovePosition(new Vector3(suivant.x, pos.y, suivant.z));

        // Arrivé, ou trop long à arriver on abandonne la cible
        tempsAvantAbandon -= Time.fixedDeltaTime;
        float distance = Vector3.Distance(new Vector3(rb.position.x, 0f, rb.position.z), cibleXZ);
        if (distance <= 0f || tempsAvantAbandon <= 0f)
            cible = null;
    }

    void LateUpdate()
    {
        float taille = Taille;
        Vector3 pos = transform.position;

        Vector3 delta = pos - dernierePos;
        delta.y = 0f;
        dernierePos = pos;

        if (enRoulement)
        {
            // L'angle suit la distance parcourue
            theta += (delta.magnitude / taille) * 90f;

            // Bloqué contre un mur : on termine l'animation sur place
            if (rouleSurPlace && cible == null)
                theta += (vitesse / taille) * 90f * Time.deltaTime;

            if (theta >= 90f)
            {
                theta = 90f;
                rouleSurPlace = false;
            }

            // Roulement terminé
            if (cible == null && !rouleSurPlace && delta.sqrMagnitude < 1e-8f)
            {
                enRoulement = false;
            }
        }

        // Je recale DOUCEMENT le visuel sur la grille
        residuHauteur = Mathf.MoveTowards(residuHauteur, 0f, taille * (vitesseRecalage / 90f) * Time.deltaTime);
        correction = Quaternion.RotateTowards(correction, Quaternion.identity, vitesseRecalage * Time.deltaTime);

        visuel.rotation = RotationVisuel();
        visuel.position = pos + Vector3.up * (residuHauteur + DecalageHauteur(theta, taille));
    }


    private Vector3 LireDirection()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return Vector3.zero;

        if (k[avancer].isPressed) return Vector3.forward;
        if (k[reculer].isPressed) return Vector3.back;
        if (k[gauche].isPressed) return Vector3.left;
        if (k[droite].isPressed) return Vector3.right;
        return Vector3.zero;
    }

    private void DemarrerDeplacement(Vector3 dir)
    {
        float taille = Taille;
        Quaternion nouvelle = Quaternion.LookRotation(dir, Vector3.up);

        // Fige l'orientation actuelle du visuel
        Rebaser(nouvelle);

        // Redresse le collider
        transform.rotation = nouvelle;
        rotRef = nouvelle;
        direction = dir;

        // Calcule la cible
        Vector3 depart = rb.position;
        float distanceLibre = DistanceLibre(depart, dir, taille);
        if (CordeBloque(dir)) distanceLibre = 0f;   // corde tendue : le cube roule sur place
        rouleSurPlace = distanceLibre < taille - 0.001f;
        cible = depart + dir * distanceLibre;

        vitesse = Mathf.Lerp(vitesseMax, vitesseMin, (taille - 1f) / 4f);
        tempsAvantAbandon = (taille / vitesse) * 1.1f;

        // lance l'animation de roulement
        axeRoulis = Vector3.Cross(Vector3.up, dir);
        enRoulement = true;
    }

    /////////////////////////////////////////////////////// Partie détection ///////////////////////////////////////////////////////

    private bool EstSoi(Collider c) => c.transform.IsChildOf(transform);

    private void BoiteTest(Vector3 centre, float taille, out Vector3 centreBoite, out Vector3 demi)
    {
        float demiHorizontal = taille * 0.5f * facteurBoite;

        float bas = -taille * 0.5f + taille * hauteurMontable;   // relatif au centre du cube
        float haut = taille * 0.5f * facteurBoite;
        float demiVertical = Mathf.Max(0.001f, (haut - bas) * 0.5f);

        centreBoite = centre + Vector3.up * ((bas + haut) * 0.5f);
        demi = new Vector3(demiHorizontal, demiVertical, demiHorizontal);
    }

    private float DistanceLibre(Vector3 depart, Vector3 dir, float taille)
    {
        BoiteTest(depart, taille, out Vector3 centreBoite, out Vector3 demi);

        int n = Physics.BoxCastNonAlloc(centreBoite, demi, dir, bufferHits,
            Quaternion.identity, taille, solides, QueryTriggerInteraction.Ignore);

        float ecart = taille * 0.5f * (1f - facteurBoite);
        float libre = taille;

        for (int i = 0; i < n; i++)
        {
            if (EstSoi(bufferHits[i].collider)) continue;

            float d = Mathf.Max(0f, bufferHits[i].distance + ecart - 0.001f);
            libre = Mathf.Min(libre, d);
        }

        return libre;
    }

    private bool CordeBloque(Vector3 dir)
    {
        if (corde == null || corde.TensionBrute < tensionMax) return false;

        Vector3 versAutre = corde.Bout(!isOnFirstBone).position - transform.position;
        versAutre.y = 0f;

        return Vector3.Dot(dir, versAutre) <= 0f;
    }

    /////////////////////////////////////////////////////// Partie animation ///////////////////////////////////////////////////////

    private Quaternion RotationVisuel()
    {
        Quaternion deviation = transform.rotation * Quaternion.Inverse(rotRef);
        return deviation * Quaternion.AngleAxis(theta, axeRoulis) * correction * baseRot;
    }

    private static float DecalageHauteur(float thetaDeg, float a)
    {
        float t = thetaDeg * Mathf.Deg2Rad;
        return a / Mathf.Sqrt(2f) * Mathf.Sin(t + Mathf.PI / 4f) - a / 2f;
    }

    private void Rebaser(Quaternion repere)
    {
        Quaternion actuelle = RotationVisuel();

        residuHauteur += DecalageHauteur(theta, Taille);
        theta = 0f;

        baseRot = ArrondirA90(actuelle, repere);
        correction = actuelle * Quaternion.Inverse(baseRot);
        rotRef = transform.rotation;
    }

    private static Quaternion ArrondirA90(Quaternion q, Quaternion repere)
    {
        Quaternion local = Quaternion.Inverse(repere) * q;

        Vector3 avant = AxeLePlusProche(local * Vector3.forward);
        Vector3 haut = local * Vector3.up;
        haut -= avant * Vector3.Dot(haut, avant);   // rend "haut" perpendiculaire à "avant"

        return repere * Quaternion.LookRotation(avant, AxeLePlusProche(haut));
    }

    private static Vector3 AxeLePlusProche(Vector3 v)
    {
        float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);

        if (ax >= ay && ax >= az) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
        if (ay >= az) return new Vector3(0f, Mathf.Sign(v.y), 0f);
        return new Vector3(0f, 0f, Mathf.Sign(v.z));
    }

    /////////////////////////////////////////////////////// Partie méthodes publiques ///////////////////////////////////////////////////////

    public void respawn()
    {
        // Déplacement
        cible = null;
        direction = Vector3.zero;
        enChute = false;
        rouleSurPlace = false;

        rb.position = spawnPosition;
        rb.rotation = spawnRotation;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);   // évite un frame de retard
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        dernierY = spawnPosition.y;

        // Visuel
        enRoulement = false;
        theta = 0f;
        residuHauteur = 0f;
        rotRef = spawnRotation;
        baseRot = baseRotSpawn;
        correction = Quaternion.identity;
        dernierePos = spawnPosition;

        OnRespawn?.Invoke();
    }
}