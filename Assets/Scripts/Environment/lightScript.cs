using UnityEngine;
using System.Collections;

public class lightScript : MonoBehaviour
{
    [SerializeField] private Light lightSource;

    [Header("Temps entre les clignotements")]
    [SerializeField] private float minOnTime = 0.05f;
    [SerializeField] private float maxOnTime = 0.5f;

    [Header("Durée de l'extinction")]
    [SerializeField] private float minOffTime = 0.02f;
    [SerializeField] private float maxOffTime = 0.2f;

    private void Start()
    {
        if (lightSource == null)
            lightSource = GetComponent<Light>();

        StartCoroutine(Flicker());
    }

    private IEnumerator Flicker()
    {
        while (true)
        {
            // Lampe allumée
            lightSource.enabled = true;

            yield return new WaitForSeconds(
                Random.Range(minOnTime, maxOnTime)
            );

            // Lampe complètement éteinte
            lightSource.enabled = false;

            yield return new WaitForSeconds(
                Random.Range(minOffTime, maxOffTime)
            );
        }
    }
}