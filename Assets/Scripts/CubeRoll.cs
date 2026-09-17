using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// À mettre sur chaque cube joueur.
// Le cube avance en "roulant" (rotation de 90° autour d'une arête).
// La distance parcourue par roulement = la taille actuelle du cube.
// => un petit cube doit rouler plus souvent qu'un gros pour la même distance.
public class CubeRoll : MonoBehaviour
{
    [Header("Contrôles (à changer pour le joueur 2)")]
    public Key avancer = Key.Z;
    public Key reculer = Key.S;
    public Key gauche = Key.Q;
    public Key droite = Key.D;

    [Header("Réglages")]
    public float dureeRoulement = 0.25f; // temps pour faire un roulement de 90°

    private bool enTrainDeRouler = false;

    void Update()
    {
        if (enTrainDeRouler) return;

        if (Keyboard.current[avancer].wasPressedThisFrame)
            StartCoroutine(Rouler(Vector3.forward));

        else if (Keyboard.current[reculer].wasPressedThisFrame)
            StartCoroutine(Rouler(Vector3.back));

        else if (Keyboard.current[gauche].wasPressedThisFrame)
            StartCoroutine(Rouler(Vector3.left));

        else if (Keyboard.current[droite].wasPressedThisFrame)
            StartCoroutine(Rouler(Vector3.right));
    }

    IEnumerator Rouler(Vector3 direction)
    {
        enTrainDeRouler = true;

        // Le cube est toujours un cube parfait : x = y = z
        float taille = transform.localScale.x;

        // Le pivot est au sol, sur l'arête dans la direction du mouvement
        Vector3 pivot = transform.position
                        + direction * (taille / 2f)
                        + Vector3.down * (taille / 2f);

        // Axe autour duquel le cube va "basculer"
        Vector3 axeRotation = Vector3.Cross(Vector3.up, direction);

        float angleTotal = 90f;
        float angleFait = 0f;

        while (angleFait < angleTotal)
        {
            float pas = (angleTotal / dureeRoulement) * Time.deltaTime;
            pas = Mathf.Min(pas, angleTotal - angleFait);
            transform.RotateAround(pivot, axeRotation, pas);
            angleFait += pas;
            yield return null;
        }

        enTrainDeRouler = false;
    }
}