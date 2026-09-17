using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CubeRoll : MonoBehaviour
{
    [Header("Contrôles")] // attention au changement AZERTY - QWERTY (j'ai fais en sorte d'avoir ZSQD et OLKM)
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

        if (Keyboard.current[avancer].wasPressedThisFrame && PeutRouler(Vector3.forward))
            StartCoroutine(Rouler(Vector3.forward));

        else if (Keyboard.current[reculer].wasPressedThisFrame && PeutRouler(Vector3.back))
            StartCoroutine(Rouler(Vector3.back));

        else if (Keyboard.current[gauche].wasPressedThisFrame && PeutRouler(Vector3.left))
            StartCoroutine(Rouler(Vector3.left));

        else if (Keyboard.current[droite].wasPressedThisFrame && PeutRouler(Vector3.right))
            StartCoroutine(Rouler(Vector3.right));
    }

    bool PeutRouler(Vector3 direction)
    {
        float taille = transform.localScale.x;

        Vector3 origine = transform.position;
        float distance = taille;

        return !Physics.BoxCast(
            origine,
            Vector3.one * (taille / 2.1f),
            direction,
            transform.rotation,
            distance
        );
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