using UnityEngine;

// À mettre sur un objet avec un Collider en "Is Trigger" qui matérialise la ligne d'arrivée.
// Compte les joueurs qui l'ont franchie, et déclenche la victoire quand les deux sont passés.
public class FinishLine : MonoBehaviour
{
    private int joueursArrives = 0;
    public int joueursRequis = 2;

    void OnTriggerEnter(Collider autre)
    {
        if (autre.CompareTag("Player"))
        {
            joueursArrives++;
            if (joueursArrives >= joueursRequis)
            {
                GameManager.Instance.Gagner();
            }
        }
    }
}