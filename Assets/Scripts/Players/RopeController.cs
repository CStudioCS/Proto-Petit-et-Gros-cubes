using UnityEngine;

public class RopeController : MonoBehaviour
{
    [Header("Corde")]
    public Transform[] os;
    public float longueurMax = 15f;

    [Header("Couleur")]
    public Material material;
    public Gradient degrade;
    [Range(0f, 1f)] public float debutTension = 0.5f;

    [Header("Tension")]
    public float forceRepos = 5f;
    public float forceMax = 100f;
    public float lissage = 10f;
    public bool afficherForce; 

    Joint[] joints;
    Rigidbody[] corps;
    Vector3[] posInit;
    Quaternion[] rotInit;
    int framesIgnorees;
    float tensionLissee;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    Color couleurOrigine;

    public float Longueur => Somme(0, os.Length - 1);
    public float Tension => tensionLissee;

    void Awake()
    {
        couleurOrigine = material.GetColor(BaseColor);
        joints = GetComponentsInChildren<Joint>();
        corps = GetComponentsInChildren<Rigidbody>();
        posInit = new Vector3[corps.Length];
        rotInit = new Quaternion[corps.Length];
        for (int i = 0; i < corps.Length; i++)
        {
            posInit[i] = corps[i].position;
            rotInit[i] = corps[i].rotation;
        }
    }

    void FixedUpdate()
    {
        if (framesIgnorees > 0)
        {
            framesIgnorees--;
            return;
        }

        float fMax = 0f;
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null) continue;
            fMax = Mathf.Max(fMax, joints[i].currentForce.magnitude);
        }

        if (afficherForce) Debug.Log($"Force max joints : {fMax:F1}");

        float brut = Mathf.InverseLerp(forceRepos, forceMax, fMax);

        TensionBrute = brut;
        tensionLissee = Mathf.Lerp(tensionLissee, brut, 1f - Mathf.Exp(-lissage * Time.fixedDeltaTime));
    }

    void LateUpdate()
    {
        if (material == null) return;

        float t = Mathf.InverseLerp(debutTension, 1f, Tension);
        material.SetColor(BaseColor, degrade.Evaluate(t));
    }

    // Longueur des segments entre les os a et b (indices)
    float Somme(int a, int b)
    {
        float l = 0f;
        for (int i = a; i < b; i++)
            l += Vector3.Distance(os[i].position, os[i + 1].position);
        return l;
    }

    public Transform Bout(bool debut) => debut ? os[0] : os[os.Length - 1];
    public Transform Voisin(bool debut) => debut ? os[1] : os[os.Length - 2];


    public void ResetCorde()
    {
        for (int i = 0; i < corps.Length; i++)
        {
            corps[i].linearVelocity = Vector3.zero;
            corps[i].angularVelocity = Vector3.zero;
            corps[i].position = posInit[i];
            corps[i].rotation = rotInit[i];
            corps[i].transform.SetPositionAndRotation(posInit[i], rotInit[i]);
        }

        Physics.SyncTransforms();

        TensionBrute = 0f;
        tensionLissee = 0f;
        framesIgnorees = 2;
    }

    void OnDestroy()
    {
        material.SetColor(BaseColor, couleurOrigine);
    }

    public float TensionBrute { get; private set; }

    void OnEnable()  { PlayerMovement.OnRespawn += ResetCorde; }
    void OnDisable() { PlayerMovement.OnRespawn -= ResetCorde; }
}