using UnityEngine;

public class Flashbang : Interactable
{
    [Header("Settings")]
    public float stunDuration = 3.0f;
    
    [Tooltip("Glisse ton fichier audio ici")]
    public AudioClip explosionSound; 
    
    [Range(0f, 1f)]
    public float volume = 1.0f;

    protected override void OnPickup(GameObject player)
    {
        // 1. Jouer le son
        // PlayClipAtPoint est parfait ici car il survit à la destruction de l'objet
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, volume);
        }

        // 2. Chercher et étourdir le Minotaure
        MinotaurAI minotaur = FindObjectOfType<MinotaurAI>();

        if (minotaur != null)
        {
            minotaur.ApplyStun(stunDuration);
            Debug.Log("BOOM! Minotaur Stunned!");
        }
    }
}