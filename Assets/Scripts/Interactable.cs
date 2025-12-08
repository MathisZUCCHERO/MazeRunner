using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnPickup(other.gameObject);
            Destroy(gameObject); // Consume item
        }
    }

    protected abstract void OnPickup(GameObject player);
}
