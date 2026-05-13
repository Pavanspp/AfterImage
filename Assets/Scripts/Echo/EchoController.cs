using UnityEngine;

/// <summary>
/// Core echo state machine. Handles Freeze (toggle) and Swap.
/// Lives on the Echo GameObject.
///
/// Freeze behaviour:
///   Following → Frozen: lock echo at current replayed position, enable collider
///   Frozen → Following: teleport echo to player, clear buffer, disable collider
///
/// Swap behaviour:
///   Teleport player to echo's current position. Echo is completely untouched —
///   no state change, no buffer clear, keeps doing exactly what it was doing.
///   If echo was Frozen: teleport player there AND unfreeze the echo.
/// </summary>
public class EchoController : MonoBehaviour
{
    [Header("References — Echo")]
    public EchoStateHub echoState;
    public EchoRecordingBuffer buffer;
    public EchoReplayer replayer;
    public EchoVisuals visuals;
    public BoxCollider2D echoCollider;

    [Header("References — Player")]
    public PlayerStateHub playerState;
    public PlayerMover playerMover;

    [Header("Overlap Check")]
    [Tooltip("Layer mask for geometry checks (walls + ground).")]
    public LayerMask solidLayers;

    [Tooltip("Size of the OverlapBox used for freeze and swap safety checks.")]
    public Vector2 checkSize = new Vector2(0.9f, 0.9f);

    // ───────────────────────────────────────────
    // FREEZE
    // ───────────────────────────────────────────

    public void ToggleFreeze()
    {
        if (echoState.State == EchoState.Following)
            Freeze();
        else
            Unfreeze();
    }

    void Freeze()
    {
        if (!replayer.IsBufferReady)
            return;

        Collider2D overlap = Physics2D.OverlapBox(
            (Vector2)transform.position, checkSize, 0f, solidLayers);
        if (overlap != null)
        {
            visuals.FlashInvalid();
            return;
        }

        echoState.State      = EchoState.Frozen;
        echoCollider.enabled = true;
        visuals.SetFrozen();
    }

    void Unfreeze()
    {
        transform.position   = playerState.Position;
        echoState.State      = EchoState.Following;
        echoCollider.enabled = false;
        buffer.Clear();
        visuals.SetFollowing();
    }

    // ───────────────────────────────────────────
    // SWAP
    // ───────────────────────────────────────────

    /// <summary>
    /// Teleport the player to the echo's current position.
    /// If echo is Following: echo is untouched, keeps replaying.
    /// If echo is Frozen: teleport player there and unfreeze.
    /// </summary>
    public void Swap()
    {
        Vector2 echoPos = (Vector2)transform.position;

        // Safety: cancel if player would land inside solid geometry
        Collider2D overlap = Physics2D.OverlapBox(echoPos, checkSize, 0f, solidLayers);
        if (overlap != null)
        {
            visuals.FlashInvalid();
            return;
        }

        playerMover.TeleportTo(echoPos);

        // If frozen, unfreeze now that the player has arrived
        if (echoState.State == EchoState.Frozen)
            Unfreeze();
    }
}