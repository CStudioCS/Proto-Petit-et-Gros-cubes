using UnityEngine;

public class LoseArea : MonoBehaviour
{
    void OnTriggerEnter(Collider autre)
    {
        //Debug.Log("contact lose avec " + autre.gameObject.tag);
        if (autre.CompareTag("Player"))
        {
            //Debug.Log( "GameManager.Instance.Perdre() appele");
            GameManager.Instance.Perdre();
        }
    }
}