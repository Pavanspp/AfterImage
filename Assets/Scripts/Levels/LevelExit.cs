using UnityEngine;

// Place this on the goal zone trigger at the end of a level.
// When the player enters the trigger, CompleteLevel is called.
//
// Setup:
//   - Add a GameObject with this script
//   - Add a BoxCollider2D or CircleCollider2D, check "Is Trigger"
//   - Set its Layer to Default (or a dedicated GoalZone layer)
//   - The player must be on a layer that collides with this trigger
public class LevelExit : MonoBehaviour
{
    [Header("Goal Zone")]
    [Tooltip("Tag of the player GameObject. Must match exactly.")]
    public string playerTag = "Player";

    [Header("Optional Visual")]
    [Tooltip("Pulse/glow effect on the goal zone — assign a child GameObject if you have one.")]
    public GameObject goalVFX;

    void Start()
    {
        if (goalVFX != null)
            goalVFX.SetActive(true);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.CompleteLevel();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.3f);

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
            return;
        }

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
        }
    }
}