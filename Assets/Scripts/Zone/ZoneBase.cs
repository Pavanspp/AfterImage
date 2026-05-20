using UnityEngine;

// Shared trigger logic for all zone types.
// Subclasses implement OnPlayerEnter/OnPlayerExit.
public abstract class ZoneBase : MonoBehaviour
{
    public string playerTag = "Player";

    protected ZoneStateTracker tracker;

    protected virtual void Awake()
    {
        GameObject player = GameObject.FindWithTag(playerTag);
        if (player != null)
            tracker = player.GetComponent<ZoneStateTracker>();
        else
            Debug.LogError("ZoneBase: No GameObject tagged 'Player' found.");

        if (tracker == null)
            Debug.LogError("ZoneBase: ZoneStateTracker not found on Player.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        OnPlayerEnter();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        OnPlayerExit();
    }

    protected abstract void OnPlayerEnter();
    protected abstract void OnPlayerExit();
}