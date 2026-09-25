using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.InputSystem;

public class MassTransfer : MonoBehaviour
{
    [Header("Références")]
    public Transform coequipier;

    [Header("Réglages")]
    public Key toucheDonnerMasse = Key.E;
    public float vitesseTransfert = 0.5f; // incrément
    public float tailleMin = 0.5f;
    public float tailleMax = 3f;
    private float tailleSpawn = 5f;

    private Rigidbody rbMoi;
    private Rigidbody rbCoequipier;

    void Start()
    {
        rbMoi = GetComponent<Rigidbody>();
        rbCoequipier = coequipier.GetComponent<Rigidbody>();
        tailleSpawn = transform.localScale.x;
    }

    void Update()
    {
        if (Keyboard.current[toucheDonnerMasse].wasPressedThisFrame)
        {
            TransfererMasse();
        }
    }

    Vector3 DeformationDir(Vector3 centre, Vector3 taille, Vector3 distance)
    {
        Vector3 posForward = centre + coequipier.forward * distance.z;
        Vector3 posBackward = centre - coequipier.forward * distance.z;
        Vector3 posRight = centre + coequipier.right * distance.x;
        Vector3 posLeft = centre - coequipier.right * distance.x;
        Vector3 posUp = centre + coequipier.up * distance.y;
        Vector3 posDown = centre - coequipier.up * distance.y;

        Vector3 deformationDir = Vector3.one;

        if (ObstaclePresent(posForward, taille) && ObstaclePresent(posBackward, taille))
            deformationDir.z = 0f;
        if (ObstaclePresent(posRight, taille) && ObstaclePresent(posLeft, taille))
            deformationDir.x = 0f;
        if (ObstaclePresent(posUp, taille) && ObstaclePresent(posDown, taille))
            deformationDir.y = 0f;

        return deformationDir;
    }

    public bool ObstaclePresent(Vector3 centre, Vector3 taille)
    {
        Collider[] colliders = Physics.OverlapBox(centre, taille / 2f, coequipier.rotation);

        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Obstacle"))
                return true;
        }

        return false;
    }

    void TransfererMasse()
    {
        float montant = vitesseTransfert;

        float mienneActuelle = Mathf.Pow(rbMoi.mass, 1f / 3f);
        float coequipierActuelle = Mathf.Pow(rbCoequipier.mass, 1f / 3f);

        montant = Mathf.Min(montant, mienneActuelle - tailleMin);
        montant = Mathf.Min(montant, tailleMax - coequipierActuelle);

        if (montant <= 0f) return;

        // coequipier
        float nouvelleTailleCoequipier = coequipierActuelle + montant;

        Vector3 deformationDirCoequipier = DeformationDir(coequipier.position, coequipier.localScale * 0.9f, coequipier.localScale * 1.5f);
        Debug.Log("DeformationDirCoequipier: " + deformationDirCoequipier);
        
        bool IsCube = Mathf.Approximately(coequipier.localScale.x, coequipier.localScale.y) && Mathf.Approximately(coequipier.localScale.y, coequipier.localScale.z);
        
        // j'empèche le transfère si le coéquipier est bloqué et qu'il est déjà déformé ou alors s'il est complètement bloqué de partout
        if ((!IsCube && deformationDirCoequipier != Vector3.one) || (deformationDirCoequipier == Vector3.zero))
            return;

        float somme = deformationDirCoequipier.x + deformationDirCoequipier.y + deformationDirCoequipier.z;
        Vector3 NewScale = deformationDirCoequipier * Mathf.Pow(nouvelleTailleCoequipier, 3f / somme) + (Vector3.one - deformationDirCoequipier) * coequipierActuelle;
        coequipier.localScale = NewScale;

        rbCoequipier.mass = Mathf.Pow(nouvelleTailleCoequipier, 3f);

        var posCoequipier = coequipier.position;
        
        Vector3 deformationDirWorld = transform.TransformDirection(NewScale);
        if (deformationDirWorld.y > 0f)
        {
            posCoequipier.y += deformationDirWorld.y / 2;
        }
        
        coequipier.position = posCoequipier;
    


        // moi
        float nouvelleTailleMoi = mienneActuelle - montant;
        
        var posMoi = transform.position;
        posMoi.y -= montant / 2;
        
        transform.localScale = Vector3.one * nouvelleTailleMoi;
        transform.position = posMoi;

        rbMoi.mass =  Mathf.Pow(nouvelleTailleMoi, 3f);
    }

    public void respawn()
    {
        transform.localScale = Vector3.one * tailleSpawn;
    }





    void OnDrawGizmosSelected()
    {
        if (coequipier == null)
            return;

        Vector3 taille = coequipier.localScale * 0.9f;
        Vector3 distance = coequipier.localScale * 1.5f;
        Quaternion rotation = coequipier.rotation;
        Vector3 centre = coequipier.position;

        // Positions testées par DeformationDir
        Vector3 posForward = centre + coequipier.forward * distance.z;
        Vector3 posBackward = centre - coequipier.forward * distance.z;
        Vector3 posRight = centre + coequipier.right * distance.x;
        Vector3 posLeft = centre - coequipier.right * distance.x;
        Vector3 posUp = centre + coequipier.up * distance.y;
        Vector3 posDown = centre - coequipier.up * distance.y;

        // Taille réelle utilisée par Physics.OverlapBox
        Vector3 boxSize = taille;

        // Avant
        Gizmos.color = ObstaclePresent(posForward, taille) ? Color.red : Color.green;
        Gizmos.matrix = Matrix4x4.TRS(posForward, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        // Arrière
        Gizmos.color = ObstaclePresent(posBackward, taille) ? Color.red : Color.green;
        Gizmos.matrix = Matrix4x4.TRS(posBackward, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        // Droite
        Gizmos.color = ObstaclePresent(posRight, taille) ? Color.red : Color.green;
        Gizmos.matrix = Matrix4x4.TRS(posRight, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        // Gauche
        Gizmos.color = ObstaclePresent(posLeft, taille) ? Color.red : Color.green;
        Gizmos.matrix = Matrix4x4.TRS(posLeft, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        // Dessus
        Gizmos.color = ObstaclePresent(posUp, taille) ? Color.red : Color.green;
        Gizmos.matrix = Matrix4x4.TRS(posUp, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        // Dessous
        Gizmos.color = ObstaclePresent(posDown, taille) ? Color.red : Color.green;
        Gizmos.matrix = Matrix4x4.TRS(posDown, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, boxSize); 

        // Remettre la matrice à zéro pour éviter des effets
        // sur d'autres Gizmos.
        Gizmos.matrix = Matrix4x4.identity;
    }
}