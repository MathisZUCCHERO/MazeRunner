using UnityEngine;

public class MinimapPickup : Interactable
{
    protected override void OnPickup(GameObject player)
    {
        // Option 1: Find a Minimap UI object and enable it
        GameObject minimap = GameObject.Find("MinimapUI");
        if (minimap != null)
        {
            // If it was disabled, we might need to search recursively or have a reference in GameManager
            // But Find only finds active objects usually. 
            // Better approach: Tell GameManager to enable minimap
        }
        
        // Simpler approach for this script: Assume the Minimap Camera exists but has a low depth or is disabled.
        // Let's assume we have a reference in GameManager or we just tag it.
        
        // Let's try activating a child object on the player if it exists (e.g., a Minimap Camera attached to player)
        Transform mapCam = player.transform.Find("MinimapCamera");
        if (mapCam != null)
        {
            mapCam.gameObject.SetActive(true);
            Debug.Log("Minimap Activated!");
        }
        else
        {
            Debug.LogWarning("MinimapCamera not found on Player!");
        }
    }
}
