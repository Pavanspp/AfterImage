using UnityEngine;

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

    void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 0.8f, 0.3f);
        Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, 
                            new Vector3(col.size.x * transform.lossyScale.x, 
                                        col.size.y * transform.lossyScale.y, 0));
    }

    protected abstract void OnPlayerEnter();
    protected abstract void OnPlayerExit();
}