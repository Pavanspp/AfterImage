using UnityEngine;

// Attach to any hazard GameObject with a trigger collider.
// On player contact, triggers the death sequence.
public class Hazard : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerMover mover = other.GetComponent<PlayerMover>();
        if (mover != null) mover.TriggerDeath();
    }
}