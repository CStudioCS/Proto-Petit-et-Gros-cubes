using UnityEngine;


public class ObstacleMover_V0 : MonoBehaviour
{
    [Header("Translation")]
    public Vector3 translationAxis = Vector3.right;
    public float distance = 5f;

    [Header("Rotation")]
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 0f; // degrés par seconde (0 = pas de rotation)

    [Header("Mouvement")]
    public float speed = 2f;
    public bool loop = true;

    private Vector3 startPos;
    private float t;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Translation : aller-retour si loop, sinon un seul trajet
        t += Time.deltaTime * speed;
        float progress = loop ? Mathf.PingPong(t, distance) : Mathf.Min(t, distance);
        transform.position = startPos + translationAxis.normalized * progress;

        // Rotation continue
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }
}









[DisallowMultipleComponent]
public class ObstacleMover : MonoBehaviour
{
    public enum LoopMode
    {
        Once,       // Le mouvement est joué une seule fois
        Loop,       // Le mouvement recommence depuis le début à chaque cycle
        PingPong    // Le mouvement fait des allers-retours
    }

    public enum AxisSpace
    {
        Local,      // Les axes suivent l'orientation initiale de l'objet
        World       // Les axes sont ceux du monde
    }

    [Header("Translation")]
    [Tooltip("Activer le déplacement")]
    public bool enableTranslation = true;
    [Tooltip("Direction du déplacement (sera normalisée). Ex : (1,0,0) = axe X")]
    public Vector3 translationAxis = Vector3.right;
    [Tooltip("Distance parcourue (en unités Unity)")]
    public float translationDistance = 5f;

    [Header("Rotation")]
    [Tooltip("Activer la rotation")]
    public bool enableRotation = false;
    [Tooltip("Axe de rotation (sera normalisé). Ex : (0,1,0) = axe Y")]
    public Vector3 rotationAxis = Vector3.up;
    [Tooltip("Angle total parcouru en degrés (360 = un tour complet)")]
    public float rotationAngle = 360f;

    [Header("Espace des axes")]
    public AxisSpace axisSpace = AxisSpace.Local;

    [Header("Temps")]
    [Tooltip("Durée d'un cycle (secondes)")]
    [Min(0.01f)] public float duration = 2f;
    [Tooltip("Délai avant le démarrage (secondes)")]
    [Min(0f)] public float startDelay = 0f;
    [Tooltip("Démarrer automatiquement au lancement")]
    public bool playOnAwake = true;

    [Header("Répétition")]
    public LoopMode loopMode = LoopMode.PingPong;

    [Header("Courbe d'animation")]
    [Tooltip("Courbe de progression (0→1). Linéaire = mouvement constant, ease = accélération/décélération")]
    public AnimationCurve easing = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    // --- État interne ---
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Rigidbody rb;
    private float elapsed;
    private bool isPlaying;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Un obstacle animé par script doit être cinématique pour pousser correctement les autres objets
            rb.isKinematic = true;
        }

        startPosition = transform.position;
        startRotation = transform.rotation;
        isPlaying = playOnAwake;
        elapsed = -startDelay;
    }

    private void Update()
    {
        // Sans Rigidbody, on bouge via le Transform dans Update
        if (rb == null) Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        // Avec Rigidbody, on bouge via la physique dans FixedUpdate
        if (rb != null) Tick(Time.fixedDeltaTime);
    }

    private void Tick(float dt)
    {
        if (!isPlaying) return;

        elapsed += dt;
        if (elapsed < 0f) return; // Délai de départ en cours

        float t = elapsed / duration;
        float progress;

        switch (loopMode)
        {
            case LoopMode.Loop:
                progress = Mathf.Repeat(t, 1f);
                break;
            case LoopMode.PingPong:
                progress = Mathf.PingPong(t, 1f);
                break;
            default: // Once
                progress = Mathf.Clamp01(t);
                if (t >= 1f) isPlaying = false;
                break;
        }

        float eased = easing.Evaluate(progress);
        Apply(eased);
    }

    private void Apply(float eased)
    {
        Vector3 pos = startPosition;
        Quaternion rot = startRotation;

        if (enableTranslation && translationAxis != Vector3.zero)
        {
            Vector3 dir = translationAxis.normalized;
            if (axisSpace == AxisSpace.Local) dir = startRotation * dir;
            pos = startPosition + dir * (translationDistance * eased);
        }

        if (enableRotation && rotationAxis != Vector3.zero)
        {
            Vector3 axis = rotationAxis.normalized;
            Quaternion delta = Quaternion.AngleAxis(rotationAngle * eased, axis);
            rot = axisSpace == AxisSpace.Local ? startRotation * delta : delta * startRotation;
        }

        if (rb != null)
        {
            rb.MovePosition(pos);
            rb.MoveRotation(rot);
        }
        else
        {
            transform.SetPositionAndRotation(pos, rot);
        }
    }

    // --- API publique (utilisable depuis d'autres scripts / UnityEvents) ---

    public void Play() => isPlaying = true;

    public void Pause() => isPlaying = false;

    /// <summary>Remet l'obstacle à sa position/rotation de départ et relance le cycle.</summary>
    public void ResetMotion()
    {
        elapsed = -startDelay;
        Apply(easing.Evaluate(0f));
    }

    // --- Aide visuelle dans l'éditeur ---
    private void OnDrawGizmosSelected()
    {
        if (!enableTranslation || translationAxis == Vector3.zero) return;

        Vector3 origin = Application.isPlaying ? startPosition : transform.position;
        Quaternion baseRot = Application.isPlaying ? startRotation : transform.rotation;

        Vector3 dir = translationAxis.normalized;
        if (axisSpace == AxisSpace.Local) dir = baseRot * dir;

        Vector3 end = origin + dir * translationDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(origin, 0.15f);
        Gizmos.DrawWireSphere(end, 0.15f);
    }
}


