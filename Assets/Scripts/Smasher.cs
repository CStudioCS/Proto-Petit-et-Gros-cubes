using UnityEngine;

public class Smasher : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            float cInit = Mathf.Pow(other.GetComponent<Rigidbody>().mass, 1f / 3f);
            float c = Mathf.Sqrt(8);
            
            if (cInit >= 3f)
                c = Mathf.Sqrt(27); // correspond à la masse d'un cube de côté 3
            
            other.transform.localScale = new Vector3 (c, 1, c);

            if (cInit > 3f)
            {
                Debug.Log("Transfert de masse vers le coéquipier : " + (cInit - 3f));
                other.GetComponent<MassTransfer>().TransfererMasse(cInit - 3f);
            }
            
            var pos = other.transform.position;
            pos.y = 0.5f;
            other.transform.position = pos;
        }
    }
}
