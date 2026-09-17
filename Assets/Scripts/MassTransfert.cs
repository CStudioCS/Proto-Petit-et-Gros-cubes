using UnityEngine;
using UnityEngine.InputSystem;


// À mettre sur chaque cube joueur.
// En maintenant la touche, le joueur donne de sa taille à son coéquipier :
// lui rétrécit, l'autre grossit, au même rythme.
public class MassTransfer : MonoBehaviour
{
    [Header("Coéquipier (glisser l'autre cube ici dans l'inspecteur)")]
    public Transform coequipier;

    [Header("Réglages")]
    public Key toucheDonnerMasse = Key.E;
    public float vitesseTransfert = 0.5f; // incrément
    public float tailleMin = 0.5f;
    public float tailleMax = 3f;

    void Update()
    {
        if (Keyboard.current[toucheDonnerMasse].wasPressedThisFrame)
        {
            TransfererMasse();
        }
    }

    void TransfererMasse()
    {
        float montant = vitesseTransfert;

        float mienneActuelle = transform.localScale.x;
        float coequipierActuelle = coequipier.localScale.x;

        montant = Mathf.Min(montant, mienneActuelle - tailleMin);
        montant = Mathf.Min(montant, tailleMax - coequipierActuelle);

        if (montant <= 0f) return;

        float nouvelleTailleMoi = mienneActuelle - montant;
        float nouvelleTailleCoequipier = coequipierActuelle + montant;

        var posMoi = transform.position;
        posMoi.y = nouvelleTailleMoi / 2;

        var posCoequipier = coequipier.position;
        posCoequipier.y = nouvelleTailleCoequipier / 2;

        transform.localScale = Vector3.one * nouvelleTailleMoi;
        transform.position = posMoi;

        coequipier.localScale = Vector3.one * nouvelleTailleCoequipier;
        coequipier.position = posCoequipier;
    }
}