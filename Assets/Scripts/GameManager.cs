using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("références inGame")]
    public GameObject J1;
    public GameObject J2;

    [Header("Références UI")]
    public GameObject winScreen;
    public GameObject loseScreen;



    private bool partieTerminee = false;
    private PlayerMovement movementJ1;
    private PlayerMovement movementJ2;
    private MassTransfer massJ1;
    private MassTransfer massJ2;

    void Awake()
    {
        Instance = this;
        movementJ1 = J1.GetComponent<PlayerMovement>();
        movementJ2 = J2.GetComponent<PlayerMovement>();

        massJ1 = J1.GetComponent<MassTransfer>();
        massJ2 = J2.GetComponent<MassTransfer>();

        winScreen.SetActive(false);
        loseScreen.SetActive(false);
    }

    public void Perdre()
    {
        if (partieTerminee) return;
        partieTerminee = true;
        Debug.Log("LOSE");
        Time.timeScale = 0f;

        loseScreen.SetActive(true);
    }

    public void Gagner()
    {
        if (partieTerminee) return;
        partieTerminee = true;
        Debug.Log("WIN");
        Time.timeScale = 0f;

        winScreen.SetActive(true);
    }

    public void Rejouer()
    {
        Time.timeScale = 1f;

        partieTerminee = false;
        
        movementJ1.respawn();
        movementJ2.respawn();

        massJ1.respawn();
        massJ2.respawn();

        winScreen.SetActive(false);
        loseScreen.SetActive(false);
    }
}