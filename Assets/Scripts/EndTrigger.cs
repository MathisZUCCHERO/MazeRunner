using UnityEngine;

public class EndTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.GetComponent<MazeAgent>() != null)
            {
                return;
            }

            GameManager.Instance.WinGame();
        }
    }
}
