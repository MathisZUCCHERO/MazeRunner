using UnityEngine;

public class EndTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // If training agent is present, let it handle the win condition (reward + reset)
            // and do NOT trigger the Game Over UI/Pause.
            if (other.GetComponent<MazeAgent>() != null)
            {
                // Agent handles its own logic in OnTriggerEnter
                return;
            }

            GameManager.Instance.WinGame();
        }
    }
}
