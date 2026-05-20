using UnityEngine;

// Core echo state machine. Handles Freeze (toggle) and Teleport.
// Lives on the Echo GameObject.
//
// Freeze:   Following → Frozen: lock echo, enable collider
//           Frozen → Following: snap echo to player, clear buffer, disable collider
//           Inside Kinetic Freeze Zone: frozen echo inherits velocity and slides until hitting geometry
//
// Teleport: Move player to echo's position. Echo untouched if Following.
//           If Frozen: teleport player there and unfreeze.
//           If inside Momentum Zone: redirect player velocity to echo's direction.
//
// NOTE: Moving platform carry is handled by PlayerMover (parenting approach),
//       NOT here. EchoController only moves the echo.
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
    public TeleportVisuals teleportVisuals;

    [Header("References — Zones")]
    public ZoneStateTracker zoneState;

    [Header("Overlap Check")]
    [Tooltip("Layer mask for geometry checks (walls + ground).")]
    public LayerMask solidLayers;

    [Tooltip("Size of the OverlapBox used for freeze and teleport safety checks.")]
    public Vector2 checkSize = new Vector2(0.9f, 0.9f);

    [Header("Teleport Cooldown")]
    public float teleportCooldown = 1f;
    float teleportCooldownTimer;

    // Kinetic slide state
    bool isKineticSliding;
    Vector2 kineticVelocity;
    int kineticSlideFrame;

    // Public read-only for PlayerMover platform detection
    public bool IsKineticSliding => isKineticSliding;
    public Vector2 KineticVelocity => kineticVelocity;

    // ───────────────────────────────────────────
    // LIFECYCLE
    // ───────────────────────────────────────────

    void Awake()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            transform.position = player.transform.position;
    }

    void Update()
    {
        if (teleportCooldownTimer > 0f)
            teleportCooldownTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (!isKineticSliding) return;
        if (echoState.State != EchoState.Frozen) return;

        kineticSlideFrame++;

        Vector2 currentPos = (Vector2)transform.position;
        Vector2 nextPos = currentPos + kineticVelocity * Time.fixedDeltaTime;

        // Skip collision for first 3 frames so echo clears any geometry
        // it was touching at freeze moment.
        if (kineticSlideFrame > 3)
        {
            // Shrink collision box perpendicular to travel direction.
            // Prevents horizontal slides catching the floor beneath,
            // or vertical slides catching walls beside.
            Vector2 dir = kineticVelocity.normalized;
            Vector2 slideCheckSize = echoCollider.size * 0.8f;

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                slideCheckSize.y *= 0.6f;
            else
                slideCheckSize.x *= 0.6f;

            Collider2D hit = Physics2D.OverlapBox(nextPos, slideCheckSize, 0f, solidLayers);
            if (hit != null && hit != echoCollider)
            {
                isKineticSliding = false;
                kineticVelocity = Vector2.zero;
                return;
            }
        }

        transform.position = nextPos;
    }

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
        if (!replayer.IsBufferReady) return;

        Collider2D overlap = Physics2D.OverlapBox(
            (Vector2)transform.position, checkSize, 0f, solidLayers);
        if (overlap != null && overlap != echoCollider)
        {
            visuals.FlashInvalid();
            return;
        }

        echoState.State = EchoState.Frozen;
        echoCollider.enabled = true;
        visuals.SetFrozen();

        // Kinetic Freeze Zone — inside-only.
        if (zoneState != null && zoneState.isInsideKineticZone)
        {
            Vector2 slideVelocity = replayer.LastReadFrame.velocity;

            if (slideVelocity.sqrMagnitude > 0.1f)
            {
                isKineticSliding = true;
                kineticVelocity = slideVelocity;
                kineticSlideFrame = 0;
            }
        }
    }

    void Unfreeze()
    {
        Vector3 frozenPos = transform.position;

        echoState.State = EchoState.Following;
        echoCollider.enabled = false;
        isKineticSliding = false;
        kineticVelocity = Vector2.zero;

        transform.position = playerState.Position;
        buffer.Clear();
        visuals.SetFollowing();

        visuals.PlayUnfreeze(frozenPos);
    }

    // ───────────────────────────────────────────
    // TELEPORT
    // ───────────────────────────────────────────

    public void Teleport()
    {
        if (!replayer.IsBufferReady)
        {
            visuals.FlashInvalid();
            return;
        }

        if (teleportCooldownTimer > 0f)
        {
            visuals.FlashInvalid();
            return;
        }

        Vector2 echoPos = (Vector2)transform.position;

        Collider2D overlap = Physics2D.OverlapBox(echoPos, checkSize, 0f, solidLayers);
        if (overlap != null && overlap != echoCollider)
        {
            visuals.FlashInvalid();
            return;
        }

        Vector3 previousPos = playerMover.transform.position;

        playerMover.TeleportTo(echoPos);

        // Momentum Zone — only active while physically inside the zone
        if (zoneState != null && zoneState.isInsideMomentumZone)
        {
            float speed = playerState.Velocity.magnitude;
            Vector2 echoDir = replayer.LastReadFrame.velocity.normalized;

            if (echoDir.sqrMagnitude > 0.01f && speed > 0.1f)
                playerMover.SetVelocity(echoDir * speed);
            else if (echoDir.sqrMagnitude > 0.01f)
                playerMover.SetVelocity(echoDir * 12f);
        }

        teleportVisuals?.PlayTeleport(previousPos, echoPos);
        teleportCooldownTimer = teleportCooldown;

        if (echoState.State == EchoState.Frozen)
            Unfreeze();
    }
}