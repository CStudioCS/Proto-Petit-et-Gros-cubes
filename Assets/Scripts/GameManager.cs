using UnityEngine;

// À mettre sur un objet vide "GameManager" dans la scène.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private bool partieTerminee = false;

    void Awake()
    {
        Instance = this;
    }

    public void Perdre()
    {
        if (partieTerminee) return;
        partieTerminee = true;
        Debug.Log("PERDU : un joueur a été distancé par la caméra !");
        Time.timeScale = 0f;
    }

    public void Gagner()
    {
        if (partieTerminee) return;
        partieTerminee = true;
        Debug.Log("GAGNÉ : la ligne d'arrivée est atteinte !");
        Time.timeScale = 0f;
    }
}