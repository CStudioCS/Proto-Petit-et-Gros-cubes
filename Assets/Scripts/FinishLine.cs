using UnityEngine;

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