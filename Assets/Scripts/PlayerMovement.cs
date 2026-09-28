using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cube qui roule autour de ses arêtes.
/// - Un HingeJoint est créé sur l'arête de pivot, détruit à la fin de chaque quart de tour,
///   puis recréé sur l'arête suivante tant que la touche est maintenue.
/// - Un couple constant est appliqué (réglable dans l'inspector).
/// - Si le cube heurte un obstacle (tag "Obstacle") ailleurs que sur son arête de pivot,
///   le hinge est déplacé sur l'arête supérieure de l'obstacle pour l'escalader.
/// - Le hinge est créé dès qu'au moins un point de l'arête de pivot touche un collider.
///   Il n'est refusé que si TOUTE l'arête est dans le vide : le couple est alors appliqué
///   directement au cube, qui bascule et tombe avec la physique normale.
///
/// Prérequis : Rigidbody + BoxCollider sur un cube à l'échelle uniforme, posé à plat
/// (rotation multiple de 90°) au démarrage. Créer le tag "Obstacle" dans Unity.
/// Conseil : Rigidbody > Interpolate = Interpolate.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Contrôles")]
    public Key avancer = Key.Z;
    public Key reculer = Key.S;
    public Key gauche = Key.Q;
    public Key droite = Key.D;

    [Header("Mouvement")]
    [Tooltip("Couple constant appliqué tant qu'une touche est maintenue (N·m). " +
             "Il doit dépasser masse * 9.81 * demi-côté pour que le cube décolle de son arête.")]
    public float couple = 15f;

    [Tooltip("Vitesse angulaire maximale (rad/s), simple sécurité.")]
    public float vitesseAngulaireMax = 10f;

    [Header("Obstacles")]
    [Tooltip("Exiger le tag ci-dessous sur l'obstacle (ou sur l'un de ses parents).")]
    public bool exigerTag = true;
    public string tagObstacle = "Obstacle";

    [Tooltip("Hauteur max franchissable, en fraction du côté du cube. Rester en dessous de 0.5 : " +
             "au-delà le centre de gravité passe au-dessus du vide et le cube retombe en arrière.")]
    [Range(0.05f, 1f)] public float hauteurMaxObstacle = 0.5f;

    [Header("Détection du vide")]
    [Tooltip("Épaisseur (unités) sous le point testé dans laquelle on cherche un collider de support.")]
    public float toleranceSol = 0.15f;
    public LayerMask couchesSol = ~0;

    [Header("Grille")]
    [Tooltip("Taille d'une case (unités monde). À la fin de chaque rotation, le centre du cube est recalé " +
             "sur un multiple de cette valeur en X et Z, et son orientation sur les axes du monde. " +
             "Mettre le côté du cube (1 = valeurs entières).")]
    public float tailleCase = 1f;

    [Header("Recalage continu (forces)")]
    [Tooltip("Raideur du rappel en rotation vers les axes monde (rad/s² par radian d'erreur). 0 = désactivé.")]
    public float raideurRotation = 40f;

    [Tooltip("Amortissement du rappel en rotation (1/s). Environ 2 * racine(raideur) pour un retour sans oscillation.")]
    public float amortissementRotation = 10f;

    [Tooltip("Raideur du rappel en position vers le centre de la case (m/s² par mètre d'erreur). 0 = désactivé. " +
             "Le frottement du sol limite la précision : une raideur trop faible laisse un petit écart résiduel.")]
    public float raideurPosition = 50f;

    [Tooltip("Amortissement du rappel en position (1/s).")]
    public float amortissementPosition = 12f;

    [Range(0f, 1f)]
    [Tooltip("Part du rappel en rotation conservée quand une touche est maintenue SANS hinge (basculement dans le vide, " +
             "cube qui roule librement). 0 = aucun rappel, 1 = rappel complet (peut empêcher de basculer dans le vide).")]
    public float facteurPendantPoussee = 0.3f;

    [Tooltip("Ancien comportement : téléporte le cube sur la case et les axes à la fin de chaque quart de tour. " +
             "Désactivé par défaut : seules les forces recalent le cube.")]
    public bool recalageInstantaneFinRoulement = false;

    [Header("Debug")]
    [Tooltip("Affiche les logs de diagnostic dans la console.")]
    public bool logs = true;
    [Tooltip("Durée (s) sans progression avant d'afficher un avertissement de blocage.")]
    public float delaiAlerteBlocage = 1f;

    // --- État interne ---
    Rigidbody rb;
    BoxCollider box;
    HingeJoint hinge;

    Vector3 directionDemandee;    // direction voulue par les touches (dernier FixedUpdate)
    Vector3 directionRoulement;   // direction monde (axe X ou Z) du roulement en cours
    Vector3 axeRoulement;         // axe de rotation monde = up x direction
    Vector3 pivotMonde;           // position monde du pivot actuel
    Quaternion rotationDepart;    // orientation (alignée) au début du quart de tour
    bool surObstacle;             // le hinge est-il sur l'arête d'un obstacle ?

    bool changementPivotEnAttente;
    Vector3 pivotEnAttente;
    Vector3 directionEnAttente;

    float demiCote;
    readonly Collider[] colliders = new Collider[16];

    // Surveillance du blocage
    float tempsBloque;
    float dernierAngle;
    bool avertiBloque;
    string dernierLog = "";

    const float SeuilFinRoulement = 90f;   // angle (°) à partir duquel le quart de tour est terminé
    const float SeuilRetour = 2f;          // angle (°) en dessous duquel on considère être revenu au départ
    const float SeuilAlignement = 0.996f;  // cos(5°) : tolérance d'alignement sur les axes monde

    static readonly Vector3[] Directions =
        { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        box = GetComponent<BoxCollider>();
        demiCote = box.size.x * transform.lossyScale.x * 0.5f;
        Log($"Init : côté du cube = {2f * demiCote:F2}, hauteur max d'obstacle = {hauteurMaxObstacle * 2f * demiCote:F2}");
    }

    // =====================================================================
    //  Boucle principale
    // =====================================================================
    void FixedUpdate()
    {
        rb.maxAngularVelocity = vitesseAngulaireMax;

        Vector3 dir = LireDirection();
        directionDemandee = dir;

        if (changementPivotEnAttente)
            AppliquerChangementPivot();

        if (hinge != null)
            GererRoulement(dir);

        if (hinge == null && dir != Vector3.zero)
            TenterDemarrageRoulement(dir);

        // Couple constant.
        if (dir != Vector3.zero)
        {
            if (hinge == null || dir == directionRoulement)
            {
                // Sans hinge (vide, cube en l'air, mal aligné) : couple direct sur le cube.
                Vector3 axe = Vector3.Cross(Vector3.up, dir);
                rb.AddTorque(axe * couple, ForceMode.Force);
            }
            else
            {
                // Autre touche pendant un roulement (ex. S alors qu'on avançait) : on inverse le couple
                // sur le même hinge, quel que soit l'angle, pour ramener le cube à plat sur son arête
                // de départ (il est alors libéré et repart dans la nouvelle direction).
                // C'est ce qui débloque un cube coincé contre un mur trop haut.
                LogUnique("Autre touche pendant le roulement : couple INVERSÉ pour revenir à plat.");
                rb.AddTorque(-axeRoulement * couple, ForceMode.Force);
            }
        }

        // Recalage continu par forces (uniquement sans hinge : le hinge impose déjà le mouvement).
        if (hinge == null && !changementPivotEnAttente)
            AppliquerRecalageContinu(dir != Vector3.zero);

        SurveillerBlocage(dir);
    }

    // =====================================================================
    //  Entrées
    // =====================================================================
    Vector3 LireDirection()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return Vector3.zero;

        // Si on roule déjà et que la touche est toujours maintenue, on garde la direction.
        if (hinge != null && Pressee(kb, directionRoulement))
            return directionRoulement;

        foreach (Vector3 d in Directions)
            if (Pressee(kb, d)) return d;

        return Vector3.zero;
    }

    bool Pressee(Keyboard kb, Vector3 d)
    {
        if (d == Vector3.forward) return kb[avancer].isPressed;
        if (d == Vector3.back)    return kb[reculer].isPressed;
        if (d == Vector3.left)    return kb[gauche].isPressed;
        if (d == Vector3.right)   return kb[droite].isPressed;
        return false;
    }

    // =====================================================================
    //  Gestion du roulement (hinge existant)
    // =====================================================================
    void GererRoulement(Vector3 dir)
    {
        float angle = AngleRoulement();

        // Quart de tour terminé : on recale proprement et on détruit le hinge.
        if (angle >= SeuilFinRoulement)
        {
            Log($"Quart de tour terminé (angle={angle:F1}°, surObstacle={surObstacle}).");
            TerminerRoulement(90f, dir != Vector3.zero);
            return;
        }

        // Touche relâchée (ou autre direction) et cube revenu à plat sur son arête de départ.
        if (dir != directionRoulement && angle < SeuilRetour)
        {
            Log($"Retour au point de départ (angle={angle:F1}°) : hinge détruit.");
            TerminerRoulement(0f, dir != Vector3.zero);
        }
    }

    float AngleRoulement()
    {
        Quaternion delta = rb.rotation * Quaternion.Inverse(rotationDepart);
        delta.ToAngleAxis(out float a, out Vector3 ax);
        if (a > 180f) a -= 360f;
        if (Vector3.Dot(ax, axeRoulement) < 0f) a = -a;
        return a;
    }

    void TerminerRoulement(float angleCible, bool continuer)
    {
        if (recalageInstantaneFinRoulement)
        {
            // Téléportation exacte sur la case, orientation alignée sur les axes monde.
            Quaternion cible = Quaternion.AngleAxis(angleCible, axeRoulement) * rotationDepart;
            Quaternion q = cible * Quaternion.Inverse(rb.rotation);
            rb.position = pivotMonde + q * (rb.position - pivotMonde);
            rb.rotation = cible;

            Vector3 c = CentreMonde();
            Vector3 centreCase = CentreGrille(c);
            rb.position += centreCase - c;
            Log($"Recalage instantané sur la case : centre={centreCase}.");
        }

        DetruireHinge();

        if (!continuer) ArreterCube();
    }

    // =====================================================================
    //  Création / destruction du hinge
    // =====================================================================
    void TenterDemarrageRoulement(Vector3 dir)
    {
        if (!EstAligne())
        {
            LogUnique("Pas de hinge : cube non aligné sur les axes monde -> couple direct.");
            return;
        }
        if (!EstAuSol())
        {
            LogUnique("Pas de hinge : cube pas au sol -> couple direct.");
            return;
        }

        // Le pivot est calculé depuis le centre de la CASE (et non le centre réel) : les petites erreurs
        // de position ne s'accumulent donc pas d'un quart de tour à l'autre.
        Vector3 centre = CentreGrille(CentreMonde());
        Vector3 pivot = centre + dir * demiCote - Vector3.up * demiCote; // arête basse avant

        // Pas de hinge uniquement si TOUTE l'arête de pivot est dans le vide.
        if (!AreteSupportee(pivot, Vector3.Cross(Vector3.up, dir)))
        {
            LogUnique($"Pas de hinge : toute l'arête de pivot {pivot} est dans le VIDE -> couple direct.");
            return;
        }

        directionRoulement = dir;
        axeRoulement = Vector3.Cross(Vector3.up, dir);
        rotationDepart = AlignerSurAxes(rb.rotation);

        CreerHinge(pivot);
        surObstacle = false;
        Log($"Hinge SOL créé : dir={dir}, pivot={pivot}");
    }

    void CreerHinge(Vector3 pivot)
    {
        Quaternion inv = Quaternion.Inverse(rb.rotation);
        Vector3 s = transform.lossyScale;

        Vector3 ancre = inv * (pivot - rb.position);
        ancre = new Vector3(ancre.x / s.x, ancre.y / s.y, ancre.z / s.z);

        hinge = gameObject.AddComponent<HingeJoint>();
        hinge.autoConfigureConnectedAnchor = false;
        hinge.connectedBody = null;            // accroché au monde
        hinge.anchor = ancre;                  // espace local du cube
        hinge.connectedAnchor = pivot;         // espace monde (pas de connectedBody)
        hinge.axis = inv * axeRoulement;       // espace local du cube
        hinge.useLimits = false;
        hinge.useMotor = false;
        hinge.useSpring = false;

        pivotMonde = pivot;
        tempsBloque = 0f;
        avertiBloque = false;
        dernierAngle = 0f;
        rb.WakeUp();
    }

    void DetruireHinge()
    {
        // DestroyImmediate : avec Destroy(), l'ancien hinge existerait encore pendant le
        // pas de physique suivant et serait actif en même temps que le nouveau.
        if (hinge != null) DestroyImmediate(hinge);
        hinge = null;
        surObstacle = false;
    }

    void AppliquerChangementPivot()
    {
        changementPivotEnAttente = false;

        bool avaitHinge = hinge != null;
        if (avaitHinge) DestroyImmediate(hinge);
        hinge = null;

        directionRoulement = directionEnAttente;
        axeRoulement = Vector3.Cross(Vector3.up, directionRoulement);
        if (!avaitHinge) rotationDepart = AlignerSurAxes(rb.rotation); // départ depuis le mode "couple direct"

        CreerHinge(pivotEnAttente);
        surObstacle = true;
        Log($"Hinge OBSTACLE créé : pivot={pivotEnAttente}, dir={directionRoulement}, avaitHinge={avaitHinge}");
    }

    // =====================================================================
    //  Obstacles
    // =====================================================================
    void OnCollisionEnter(Collision c) { TraiterCollision(c); }
    void OnCollisionStay(Collision c)  { TraiterCollision(c); } // cas où on est déjà collé à l'obstacle

    void TraiterCollision(Collision c)
    {
        if (surObstacle || changementPivotEnAttente) return;

        // Direction de référence : celle du roulement en cours, sinon celle des touches.
        Vector3 dir = hinge != null ? directionRoulement : directionDemandee;
        if (dir == Vector3.zero) return;

        Collider col = c.collider;
        float cote = 2f * demiCote;
        Vector3 centre = CentreMonde();
        Vector3 pivotSol = hinge != null ? pivotMonde : centre + dir * demiCote - Vector3.up * demiCote;
        Bounds b = col.bounds;

        // Sol, ou support sur lequel on est posé : rien à faire.
        // (On ne filtre PAS sur la normale du contact : quand le cube est incliné, il touche
        //  l'obstacle par une face inclinée dont la normale n'est plus horizontale.)
        float hauteur = b.max.y - pivotSol.y;
        if (hauteur <= 0.02f) return;

        if (exigerTag && !PorteTag(col.transform, tagObstacle))
        {
            LogUnique($"Collision avec '{col.name}' IGNORÉE : tag='{col.tag}' (attendu '{tagObstacle}').");
            return;
        }

        if (hinge == null && (!EstAligne() || !EstAuSol()))
        {
            LogUnique($"Collision avec '{col.name}' sans hinge : cube non aligné ou pas au sol, ignorée.");
            return;
        }

        // --- L'obstacle est-il devant le cube (dans la direction de marche) et à sa largeur ? ---
        bool dirEstX = Mathf.Abs(dir.x) > 0.5f;
        float signe = dirEstX ? dir.x : dir.z;
        float faceProche = signe > 0f ? (dirEstX ? b.min.x : b.min.z)
                                      : (dirEstX ? b.max.x : b.max.z);
        float frontCube = dirEstX ? pivotSol.x : pivotSol.z;
        float ecartFace = (faceProche - frontCube) * signe;   // > 0 : la face de l'obstacle est devant le pivot

        float latCentre = dirEstX ? centre.z : centre.x;
        float latMin = dirEstX ? b.min.z : b.min.x;
        float latMax = dirEstX ? b.max.z : b.max.x;
        float chevauchement = Mathf.Min(latCentre + demiCote, latMax) - Mathf.Max(latCentre - demiCote, latMin);

        if (ecartFace < -0.3f * cote)
        {
            LogUnique($"Obstacle '{col.name}' ignoré : il est DERRIÈRE le pivot (écart={ecartFace:F2}).");
            return;
        }
        if (chevauchement <= 0.1f * cote)
        {
            LogUnique($"Obstacle '{col.name}' ignoré : sur le CÔTÉ (chevauchement latéral={chevauchement:F2}).");
            return;
        }

        // --- Hauteur franchissable ? ---
        float hauteurMax = hauteurMaxObstacle * cote;
        if (hauteur > hauteurMax + 0.01f)
        {
            LogUnique($"Obstacle '{col.name}' TROP HAUT : hauteur={hauteur:F2} > max={hauteurMax:F2} " +
                      $"(réduire l'obstacle ou augmenter hauteurMaxObstacle ; ne pas dépasser 0.5 du côté du cube).");
            return;
        }

        // --- Arête haute de l'obstacle, côté joueur ---
        Vector3 p = centre;
        if (dirEstX) p.x = faceProche; else p.z = faceProche;
        p.y = b.max.y;

        // --- Pas de hinge uniquement si TOUTE l'arête de pivot est dans le vide ---
        if (!AreteSupportee(p, Vector3.Cross(Vector3.up, dir)))
        {
            LogUnique($"Obstacle '{col.name}' : toute l'arête de pivot {p} est dans le VIDE -> pivot refusé.");
            return;
        }

        string infoAngle = hinge != null ? $"{AngleRoulement():F1}°" : "n/a (sans hinge)";
        Vector3 n0 = c.contactCount > 0 ? c.GetContact(0).normal : Vector3.zero;
        Log($"Obstacle '{col.name}' ACCEPTÉ : hauteur={hauteur:F2}, écart face={ecartFace:F2}, " +
            $"angle du cube={infoAngle}, normale contact={n0}, nouveau pivot={p}.");

        changementPivotEnAttente = true;
        pivotEnAttente = p;
        directionEnAttente = dir;
    }

    static bool PorteTag(Transform t, string tag)
    {
        // Le tag peut être sur le collider ou sur l'un de ses parents.
        while (t != null)
        {
            if (t.gameObject.tag == tag) return true;
            t = t.parent;
        }
        return false;
    }

    // =====================================================================
    //  Utilitaires
    // =====================================================================
    Vector3 CentreMonde()
    {
        return rb.position + rb.rotation * Vector3.Scale(box.center, transform.lossyScale);
    }

    // =====================================================================
    //  Recalage continu (rappels type ressort amorti)
    // =====================================================================
    void AppliquerRecalageContinu(bool pousse)
    {
        AppliquerRecalageRotation(pousse);
        if (!pousse) AppliquerRecalagePosition();
    }

    /// <summary>Couple de rappel vers l'orientation alignée sur les axes monde la plus proche.</summary>
    void AppliquerRecalageRotation(bool pousse)
    {
        if (raideurRotation <= 0f) return;

        Quaternion cible = AlignerSurAxes(rb.rotation);
        Quaternion erreur = cible * Quaternion.Inverse(rb.rotation);
        erreur.ToAngleAxis(out float angleDeg, out Vector3 axe);
        if (angleDeg > 180f) angleDeg -= 360f;              // plus court chemin
        if (Mathf.Abs(angleDeg) < 0.1f) return;             // assez aligné : on laisse le corps se rendormir

        Vector3 accel = axe * (angleDeg * Mathf.Deg2Rad * raideurRotation);
        if (pousse)
            accel *= facteurPendantPoussee;                 // ne pas empêcher le basculement dans le vide
        else
            accel -= rb.angularVelocity * amortissementRotation;

        rb.AddTorque(accel, ForceMode.Acceleration);
    }

    /// <summary>Force de rappel horizontale vers le centre de la case la plus proche.</summary>
    void AppliquerRecalagePosition()
    {
        if (raideurPosition <= 0f) return;
        if (!EstAligne() || !EstAuSol()) return;            // d'abord la rotation, et jamais en l'air

        Vector3 c = CentreMonde();
        Vector3 cible = CentreGrille(c);
        Vector3 erreur = new Vector3(cible.x - c.x, 0f, cible.z - c.z);
        if (erreur.magnitude < 0.003f) return;

        Vector3 v = VitesseLineaire;
        Vector3 vHoriz = new Vector3(v.x, 0f, v.z);

        Vector3 accel = erreur * raideurPosition - vHoriz * amortissementPosition;
        rb.AddForce(accel, ForceMode.Acceleration);
    }

    /// <summary>Centre de la case la plus proche (X et Z arrondis à tailleCase, Y inchangé).</summary>
    Vector3 CentreGrille(Vector3 c)
    {
        float t = Mathf.Max(0.0001f, tailleCase);
        return new Vector3(Mathf.Round(c.x / t) * t, c.y, Mathf.Round(c.z / t) * t);
    }

    Vector3 VitesseLineaire
    {
        get
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }
    }

    /// <summary>Arrondit une orientation (déjà proche) au multiple de 90° le plus proche sur chaque axe monde.</summary>
    static Quaternion AlignerSurAxes(Quaternion r)
    {
        Vector3 f = AxeLePlusProche(r * Vector3.forward);
        Vector3 u = AxeLePlusProche(r * Vector3.up);
        if (Mathf.Abs(Vector3.Dot(f, u)) > 0.5f) return r; // sécurité : ne devrait pas arriver si EstAligne()
        return Quaternion.LookRotation(f, u);
    }

    static Vector3 AxeLePlusProche(Vector3 v)
    {
        float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
        if (ax >= ay && ax >= az) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
        if (ay >= az)             return new Vector3(0f, Mathf.Sign(v.y), 0f);
        return new Vector3(0f, 0f, Mathf.Sign(v.z));
    }

    bool EstAligne()
    {
        Quaternion r = rb.rotation;
        return AxeAligne(r * Vector3.right) && AxeAligne(r * Vector3.up) && AxeAligne(r * Vector3.forward);
    }

    static bool AxeAligne(Vector3 v)
    {
        float m = Mathf.Max(Mathf.Abs(v.x), Mathf.Max(Mathf.Abs(v.y), Mathf.Abs(v.z)));
        return m > SeuilAlignement;
    }

    /// <summary>
    /// L'arête de pivot touche-t-elle quelque chose ? Vrai dès qu'UN SEUL point de l'arête est en contact
    /// avec un collider ; faux seulement si toute l'arête (sur la largeur du cube) est dans le vide.
    /// Une seule requête couvre toute l'arête : un obstacle plus étroit que le cube suffit.
    /// </summary>
    bool AreteSupportee(Vector3 pivot, Vector3 axe)
    {
        bool axeX = Mathf.Abs(axe.x) > 0.5f;
        float longueur = demiCote * 0.95f;   // le long de l'arête
        const float profondeurArete = 0.05f; // perpendiculairement à l'arête
        float epaisseurV = toleranceSol + 0.02f;

        Vector3 centreBoite = new Vector3(pivot.x, pivot.y - toleranceSol * 0.5f + 0.01f, pivot.z);
        Vector3 demi = new Vector3(axeX ? longueur : profondeurArete, epaisseurV * 0.5f,
                                   axeX ? profondeurArete : longueur);
        return SupportDansBoite(centreBoite, demi);
    }

    /// <summary>Le cube repose-t-il sur quelque chose ? Un seul point de sa face inférieure suffit.</summary>
    bool EstAuSol()
    {
        Vector3 c = CentreMonde();
        float r = demiCote * 0.95f;
        float epaisseurV = toleranceSol + 0.02f;

        Vector3 centreBoite = new Vector3(c.x, c.y - demiCote - toleranceSol * 0.5f + 0.01f, c.z);
        return SupportDansBoite(centreBoite, new Vector3(r, epaisseurV * 0.5f, r));
    }

    /// <summary>Y a-t-il un collider (autre que le joueur) dans cette boîte ? Fonctionne aussi depuis l'intérieur d'un obstacle.</summary>
    bool SupportDansBoite(Vector3 centreBoite, Vector3 demiExtensions)
    {
        int n = Physics.OverlapBoxNonAlloc(centreBoite, demiExtensions, colliders, Quaternion.identity,
                                           couchesSol, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (colliders[i].attachedRigidbody != rb) return true;
        return false;
    }

    void ArreterCube()
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
    }

    // =====================================================================
    //  Diagnostic
    // =====================================================================
    void SurveillerBlocage(Vector3 dir)
    {
        if (hinge == null || dir != directionRoulement)
        {
            tempsBloque = 0f;
            avertiBloque = false;
            return;
        }

        float angle = AngleRoulement();
        if (Mathf.Abs(angle - dernierAngle) > 0.5f)
        {
            dernierAngle = angle;
            tempsBloque = 0f;
            avertiBloque = false;
            return;
        }

        tempsBloque += Time.fixedDeltaTime;
        if (logs && !avertiBloque && tempsBloque > delaiAlerteBlocage)
        {
            avertiBloque = true;
            Debug.LogWarning($"[PlayerMovement] BLOQUÉ depuis {tempsBloque:F1}s : angle={angle:F1}°, " +
                             $"surObstacle={surObstacle}, dir={directionRoulement}, pivot={pivotMonde}, " +
                             $"vitAng={rb.angularVelocity.magnitude:F2}, couple={couple}. " +
                             "Regarder les logs de collision juste avant : tag manquant, obstacle trop haut, couple trop faible ?", this);
        }
    }

    void Log(string message)
    {
        if (logs) Debug.Log("[PlayerMovement] " + message, this);
    }

    // Comme Log, mais n'affiche pas deux fois de suite le même message (évite le spam de OnCollisionStay).
    void LogUnique(string message)
    {
        if (!logs || message == dernierLog) return;
        dernierLog = message;
        Debug.Log("[PlayerMovement] " + message, this);
    }

    void OnDrawGizmos()
    {
        if (hinge == null) return;
        Gizmos.color = surObstacle ? Color.red : Color.yellow;
        Gizmos.DrawSphere(pivotMonde, 0.06f);
        Gizmos.DrawLine(pivotMonde - axeRoulement * demiCote, pivotMonde + axeRoulement * demiCote);
    }
}