using UnityEngine;

public class SpeedBoost : Interactable
{
    public float multiplier = 2.0f;
    public float duration = 5.0f;

    protected override void OnPickup(GameObject player)
    {
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ApplySpeedBoost(multiplier, duration);
            Debug.Log("Speed Boost Activated!");
        }
    }
}
