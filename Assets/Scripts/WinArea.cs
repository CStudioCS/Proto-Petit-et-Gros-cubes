using System.Collections.Generic;
using UnityEngine;

public class FinishLine : MonoBehaviour
{
    public int joueursRequis = 2;

    private readonly HashSet<GameObject> joueursArrives = new HashSet<GameObject>();

    void OnTriggerEnter(Collider autre)
    {
        if (!autre.CompareTag("Player")) return;

        // On récupère l'objet racine du joueur (au cas où le collider est sur un enfant)
        GameObject joueur = autre.attachedRigidbody != null
            ? autre.attachedRigidbody.gameObject
            : autre.gameObject;

        // Sécurité : si ce joueur est déjà arrivé, on ignore
        if (!joueursArrives.Add(joueur)) return;

        Figer(joueur);

        if (joueursArrives.Count >= joueursRequis)
        {
            GameManager.Instance.Gagner();
        }
    }

    void Figer(GameObject joueur)
    {
        PlayerMovement mouvement = joueur.GetComponent<PlayerMovement>();
        if (mouvement != null) mouvement.enabled = false;

        Rigidbody rb = joueur.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Animator anim = joueur.GetComponent<Animator>();
        if (anim != null) anim.speed = 0f;
    }

    public void ResetLigne()
    {
        foreach (GameObject joueur in joueursArrives)
        {
            if (joueur == null) continue;

            PlayerMovement mouvement = joueur.GetComponent<PlayerMovement>();
            if (mouvement != null) mouvement.enabled = true;

            Rigidbody rb = joueur.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            Animator anim = joueur.GetComponent<Animator>();
            if (anim != null) anim.speed = 2f;
        }

        joueursArrives.Clear();
    }

    void OnEnable()  { PlayerMovement.OnRespawn += ResetLigne; }
    void OnDisable() { PlayerMovement.OnRespawn -= ResetLigne; }
}