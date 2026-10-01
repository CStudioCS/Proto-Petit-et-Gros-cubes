using UnityEngine;

public class RopeRenderer : MonoBehaviour
{
    [Header("Joueurs")]
    public PlayerMovement joueurA;
    public PlayerMovement joueurB;

    [Header("Forme")]
    public int segments = 24;
    public float largeurMin = 0.03f;
    public float largeurMax = 0.2f;
    public float amplitudeMax = 0.4f;

    [Header("Animation")]
    public float vitesseAnim = 8f;
    public float frequenceOnde = 3f;
    public float lissage = 10f;

    private LineRenderer lr;
    private float tensionLissee;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = segments;
    }

    void LateUpdate()
    {
        if (joueurA == null || joueurB == null) return;

        float cible = Mathf.Max(joueurA.Tension, joueurB.Tension);
        tensionLissee = Mathf.Lerp(tensionLissee, cible, 1f - Mathf.Exp(-lissage * Time.deltaTime));

        Vector3 a = joueurA.transform.position;
        Vector3 b = joueurB.transform.position;
        Vector3 axe = b - a;
        if (axe.sqrMagnitude < 0.0001f) return;
        Vector3 dir = axe.normalized;

        Vector3 perp1 = Vector3.Cross(dir, Vector3.up);
        if (perp1.sqrMagnitude < 0.001f) perp1 = Vector3.Cross(dir, Vector3.right);
        perp1.Normalize();
        Vector3 perp2 = Vector3.Cross(dir, perp1);

        float amplitude = amplitudeMax * tensionLissee;
        float temps = Time.time * vitesseAnim;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            float enveloppe = Mathf.Sin(t * Mathf.PI);

            float n1 = Mathf.PerlinNoise(t * frequenceOnde + temps, 0f) * 2f - 1f;
            float n2 = Mathf.PerlinNoise(t * frequenceOnde + temps, 10f) * 2f - 1f;

            Vector3 p = Vector3.Lerp(a, b, t)
                      + (perp1 * n1 + perp2 * n2) * (amplitude * enveloppe);
            lr.SetPosition(i, p);
        }

        // Largeur, avec une légère pulsation qui s'accentue avec la tension
        float pulsation = 1f + 0.25f * tensionLissee * Mathf.Sin(temps * 2f);
        lr.widthMultiplier = Mathf.Lerp(largeurMin, largeurMax, tensionLissee) * pulsation;

    }
}