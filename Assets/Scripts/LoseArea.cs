using UnityEngine;

public class LoseArea : MonoBehaviour
{
    void OnTriggerEnter(Collider autre)
    {
        if (autre.CompareTag("Player"))
        {
            GameManager.Instance.Perdre();
        }
    }
}