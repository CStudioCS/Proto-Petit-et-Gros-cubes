using UnityEngine;

public class LoseArea : MonoBehaviour
{
    private int joueursArrives = 0;
    public int joueursRequis = 1;

    void OnTriggerEnter(Collider autre)
    {
        if (autre.CompareTag("Player"))
        {
            joueursArrives++;
            if (joueursArrives >= joueursRequis)
            {
                GameManager.Instance.Perdre();
            }
        }
    }
}