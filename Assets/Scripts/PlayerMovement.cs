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
    public float vitesseMin = 5f;
    public float vitesseMax = 8f;

    [Header("Détection")]
    public LayerMask solides = ~0;
    [Range(0.5f, 1f)] public float facteurBoite = 0.8f;
    [Range(0f, 0.9f)] public float hauteurMontable = 0.3f;
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

    private bool rouleSurPlace;

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
        if (cible != null || enChute || rouleSurPlace)
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

            float distanceLibre = DistanceLibre(depart, direction, taille);
            rouleSurPlace = distanceLibre < taille - 0.001f;

            vitesse = Mathf.Lerp(vitesseMax, vitesseMin, (taille-1f)/4f);

            cible = depart + direction * distanceLibre;

            tempsRestantAvantAbandon = (taille / vitesse) * 1.1f; // sécu

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
            if (d < libre) libre = d;
        }

        return libre;
    }




    void LateUpdate()
    {
        float taille = transform.localScale.x;
        Vector3 pos = transform.position;

        Vector3 delta = pos - dernierePosVisuel;
        delta.y = 0f;
        dernierePosVisuel = pos;

        if (enRoulement)
        {
            theta += (delta.magnitude / taille) * 90f;

            if (rouleSurPlace && cible == null)
                theta += (vitesse / taille) * 90f * Time.deltaTime;

            if (theta >= 90f)
            {
                theta = 90f;
                rouleSurPlace = false;
            }
        }


        if (enRoulement && (nouveauRoulement || (cible == null && !rouleSurPlace && delta.sqrMagnitude < 1e-8f)))
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
        return a / Mathf.Sqrt(2f) * Mathf.Sin(t + Mathf.PI / 4f) - a / 2f; // formule de la hauteur d'un cube qui roule sur un plan
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
        u -= f * Vector3.Dot(u, f); 
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
        rouleSurPlace = false;

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