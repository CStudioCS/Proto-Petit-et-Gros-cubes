using UnityEngine;
// using UnityEngine.SceneManagement;
using System.Collections.Generic;

// CE SCRIPT PEUX ETRE GRANDEMENT OPTIMISE


public class SceneDestruction : MonoBehaviour
{
    [Header("Paramètres de destruction")]
    public float debutZ;
    public float FinZ;
    [Range(0f, 600f)]
    public float temps;
    
    [Range(0f, 30)]
    public float time_offset = 0;


    private float speed;

    private List<GameObject> sceneObjects;

    private void resetTimeOffset()
    {
        time_offset+=Time.time;
    }
    void OnEnable()
    {
        //gameObject.transform.position = new Vector3(0, 0, debutZ);

    
        PlayerMovement.OnRespawn += resetTimeOffset;
        speed = (FinZ - debutZ ) / temps;
        sceneObjects = GetAllObjectsOnlyInScene(); //couteux mais appelé juste à l'initialisatioin
    }

    void Update()
    {

        if (Time.time > time_offset)
            gameObject.transform.position += Vector3.forward * speed * Time.deltaTime;
        
        foreach(GameObject e in sceneObjects)
        {
            if( e.transform.position.z < gameObject.transform.position.z )
            {
                makeGOfall(e);
            }
            
        }

    }

    // public void OnTriggerEnter(Collider e)
    // {
    //     Debug.Log("enter");
    //     e.gameObject.SetActive(false);
    // }

    List<GameObject> GetAllObjectsOnlyInScene()
    {
        List<GameObject> objectsInScene = new List<GameObject>();

        foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
            if (go.scene.IsValid() &&  !(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave))
                if(go.transform.childCount == 0 && go.transform.parent != null && !HasMustNotFallParent(go.transform) && !(go.gameObject.tag == "MainCamera" || go.gameObject.tag == "Player" ||  go.gameObject.layer == 5))
                {
                        objectsInScene.Add(go);
                }
        }

        return objectsInScene;
    }

    bool HasMustNotFallParent(Transform transform) // pas ouf niveau optimisation
    {
        Transform parent = transform.parent;

        while (parent != null)
        {
            if (parent.CompareTag("MustNotFall"))
                return true;

            parent = parent.parent;
        }

        return false;
    }
    
    void makeGOfall(GameObject e)
    {
        Rigidbody rigidbody = e.GetComponent<Rigidbody>();
        if (rigidbody == null)
            rigidbody = e.AddComponent<Rigidbody>();
        rigidbody.useGravity = true;
        rigidbody.isKinematic = false;
    }
   
}
