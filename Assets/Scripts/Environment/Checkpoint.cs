using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Références")]
    public PlayerMovement J1;
    public PlayerMovement J2;
    public Transform pointRespawnJ1;
    public Transform pointRespawnJ2;
    public GameObject WallToBreak;

    private readonly HashSet<PlayerMovement> joueursArrives = new HashSet<PlayerMovement>();
    private bool valide;

    void OnTriggerEnter(Collider other)
    {
        if (valide) return;

        PlayerMovement joueur = other.GetComponentInParent<PlayerMovement>();
        if (joueur == null || !joueursArrives.Add(joueur)) return;

        joueur.bloque = true;

        if (joueursArrives.Count >= 2)
            Valider();
    }

    void Valider()
    {
        valide = true;

        J1.DefinirCheckpoint(pointRespawnJ1.position);
        J2.DefinirCheckpoint(pointRespawnJ2.position);

        foreach (PlayerMovement j in joueursArrives)
            if (j != null) j.bloque = false;
        joueursArrives.Clear();

        if (WallToBreak != null) WallToBreak.SetActive(false);
    }

    void ResetCheckpoint()
    {
        if (valide) return;

        foreach (PlayerMovement j in joueursArrives)
            if (j != null) j.bloque = false;
        joueursArrives.Clear();
    }

    void NouvellePartie()
    {
        valide = false;
        joueursArrives.Clear();
        if (WallToBreak != null) WallToBreak.SetActive(true);
    }

    void OnEnable()
    {
        PlayerMovement.OnRespawn += ResetCheckpoint;
        PlayerMovement.OnNouvellePartie += NouvellePartie;
    }
    void OnDisable()
    {
        PlayerMovement.OnRespawn -= ResetCheckpoint;
        PlayerMovement.OnNouvellePartie -= NouvellePartie;
    }
}