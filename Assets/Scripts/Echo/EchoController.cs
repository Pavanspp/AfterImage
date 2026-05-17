using UnityEngine;

// Core echo state machine. Handles Freeze (toggle) and Teleport.
// Lives on the Echo GameObject.
//
// Freeze:   Following → Frozen: lock echo, enable collider
//           Frozen → Following: snap echo to player, clear buffer, disable collider
//
// Teleport: Move player to echo's position. Echo untouched if Following.
//           If Frozen: teleport player there and unfreeze.
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

    [Tooltip("Size of the OverlapBox used for freeze and teleport safety checks.")]
    public Vector2 checkSize = new Vector2(0.9f, 0.9f);

    [Header("Teleport Cooldown")]
    public float teleportCooldown = 1f;
    float teleportCooldownTimer;

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
        if (overlap != null && overlap != echoCollider)
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
    // TELEPORT
    // ───────────────────────────────────────────

    void Update()
    {
        if (teleportCooldownTimer > 0f)
            teleportCooldownTimer -= Time.deltaTime;
    }

    void OnEnable()
    {
        playerState.OnDeath += HandlePlayerDeath;
    }

    void OnDisable()
    {
        playerState.OnDeath -= HandlePlayerDeath;
    }

    void HandlePlayerDeath()
    {
        if (echoState.State == EchoState.Frozen)
            Unfreeze();
    }

    // Teleport the player to the echo's current position.
    // If echo is Following: echo is untouched, keeps replaying.
    // If echo is Frozen: teleport player there and unfreeze.
    public void Teleport()
    {
        if (teleportCooldownTimer > 0f)
        {
            visuals.FlashInvalid();
            return;
        }

        Vector2 echoPos = (Vector2)transform.position;

        // Cancel if player would land inside solid geometry
        Collider2D overlap = Physics2D.OverlapBox(echoPos, checkSize, 0f, solidLayers);
        if (overlap != null && overlap != echoCollider)
        {
            visuals.FlashInvalid();
            return;
        }

        playerMover.TeleportTo(echoPos);
        teleportCooldownTimer = teleportCooldown;

        if (echoState.State == EchoState.Frozen)
            Unfreeze();
    }
}