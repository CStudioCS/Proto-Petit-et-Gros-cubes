using UnityEngine;

public class DesactiverObjets : MonoBehaviour
{
    [Header("Objets à désactiver")]
    public GameObject[] objets;

    private bool declenche = false;

    void OnTriggerEnter(Collider autre)
    {
        if (declenche) return;
        if (!autre.CompareTag("Player")) return;

        declenche = true;

        foreach (GameObject obj in objets)
        {
            if (obj != null) obj.SetActive(false);
        }
    }

    // À appeler au respawn
    public void Reactiver()
    {
        foreach (GameObject obj in objets)
        {
            if (obj != null) obj.SetActive(true);
        }

        declenche = false;
    }

    void OnEnable()  { PlayerMovement.OnRespawn += Reactiver; }
    void OnDisable() { PlayerMovement.OnRespawn -= Reactiver; }
}