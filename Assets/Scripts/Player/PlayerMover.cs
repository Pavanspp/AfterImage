using UnityEngine;
using System.Collections;

public class PlayerMover : MonoBehaviour
{
    Rigidbody2D rb;
    PlayerInputReader input;
    PlayerStateHub state;
    PlayerPathTrail pathTrail;

    [Header("Movement")]
    public float moveSpeed       = 10f;
    public float acceleration    = 60f;
    public float deceleration    = 80f;
    public float airAcceleration = 30f;
    int facing = 1;

    [Header("Jump Feel")]
    public float jumpForce         = 15f;
    public float coyoteTime        = 0.1f;
    public float jumpBufferTime    = 0.1f;
    public float jumpCutMultiplier = 0.4f;

    [Header("Wall Jump")]
    public float wallJumpForceX = 10f;
    public float wallJumpForceY = 14f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Wall Check")]
    public LayerMask wallLayer;

    [Header("Respawn")]
    public float deathFloorY = -25f;
    public float deathDelay  = 0.6f;

    float moveX;
    float coyoteTimer;
    float jumpBufferTimer;
    bool  isOnWall;
    int   wallDirection;
    bool  wasGrounded;
    bool  isJumping;
    float baseGravity;
    bool  isDead;

    const float WallCheckOffset = 0.55f;
    const float WallCheckRadius = 0.2f;
    static readonly Vector2 GroundCheckSize = new Vector2(0.8f, 0.1f);

    // ── Moving platform state ──
    // Standard approach: parent the player to the platform while standing on it.
    // When leaving, add the platform's velocity to the rigidbody so momentum carries.
    Transform currentPlatform;
    Transform originalParent;
    Vector2   platformVelocity;
    Vector3   lastPlatformPos;

    void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        input          = GetComponent<PlayerInputReader>();
        state          = GetComponent<PlayerStateHub>();
        pathTrail      = GetComponent<PlayerPathTrail>();
        baseGravity    = rb.gravityScale;
        originalParent = transform.parent; // null in most scenes, that's fine
    }

    // ───────────────────────────────────────────
    // UPDATE
    // ───────────────────────────────────────────

    void Update()
    {
        if (isDead) return;

        float rawX = input.Move.x;
        moveX = rawX > 0.01f ? 1f : rawX < -0.01f ? -1f : 0f;

        if (moveX != 0)
        {
            facing = moveX > 0 ? 1 : -1;
            transform.localScale = new Vector3(facing, 1f, 1f);
        }

        if (input.JumpPressedThisFrame)
        {
            jumpBufferTimer = jumpBufferTime;

            bool grounded  = Physics2D.OverlapBox(groundCheck.position, GroundCheckSize, 0f, groundLayer);
            bool wallRight = Physics2D.OverlapCircle(transform.position + Vector3.right * WallCheckOffset, WallCheckRadius, wallLayer | groundLayer);
            bool wallLeft  = Physics2D.OverlapCircle(transform.position + Vector3.left  * WallCheckOffset, WallCheckRadius, wallLayer | groundLayer);

            if (!grounded && (wallRight || wallLeft))
            {
                wallDirection = wallRight ? 1 : -1;
                DoWallJump();
            }
        }

        if (input.JumpReleasedThisFrame && isJumping && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            isJumping = false;
        }

        if (transform.position.y < deathFloorY)
            TriggerDeath();
    }

    // ───────────────────────────────────────────
    // FIXED UPDATE
    // ───────────────────────────────────────────

    void FixedUpdate()
    {
        if (isDead) return;

        jumpBufferTimer -= Time.fixedDeltaTime;

        bool isGrounded = Physics2D.OverlapBox(groundCheck.position, GroundCheckSize, 0f, groundLayer);
        if (isGrounded) isJumping = false;

        // ── Moving platform detection ──
        UpdatePlatformCarry(isGrounded);

        bool wallRight = Physics2D.OverlapCircle(transform.position + Vector3.right * WallCheckOffset, WallCheckRadius, wallLayer | groundLayer);
        bool wallLeft  = Physics2D.OverlapCircle(transform.position + Vector3.left  * WallCheckOffset, WallCheckRadius, wallLayer | groundLayer);

        if      (!isGrounded && wallRight) { isOnWall = true;  wallDirection =  1; }
        else if (!isGrounded && wallLeft)  { isOnWall = true;  wallDirection = -1; }
        else                               { isOnWall = false;                     }

        if (isOnWall && jumpBufferTimer > 0f)
            DoWallJump();

        if (isGrounded) coyoteTimer = coyoteTime;
        else            coyoteTimer -= Time.fixedDeltaTime;

        wasGrounded = isGrounded;

        float vx     = rb.linearVelocity.x;
        float target = moveX * moveSpeed;

        if (isGrounded)
        {
            if (Mathf.Abs(moveX) > 0.01f)
                vx = Mathf.MoveTowards(vx, target, acceleration * Time.fixedDeltaTime);
            else
                vx = Mathf.MoveTowards(vx, 0f, deceleration * Time.fixedDeltaTime);
        }
        else
        {
            if (Mathf.Abs(moveX) > 0.01f)
                vx = Mathf.MoveTowards(vx, target, airAcceleration * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        if (!isOnWall && jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer   = 0f;
            coyoteTimer       = 0f;
            isJumping         = true;
        }

        UpdateStateHub(isGrounded);
    }

    // ───────────────────────────────────────────
    // MOVING PLATFORM CARRY
    // ───────────────────────────────────────────

    void UpdatePlatformCarry(bool isGrounded)
    {
        if (isGrounded)
        {
            // Check if the ground collider we're standing on is the echo
            Collider2D groundHit = Physics2D.OverlapBox(
                groundCheck.position, GroundCheckSize, 0f, groundLayer);

            if (groundHit != null && groundHit.CompareTag("Echo"))
            {
                // Mount the platform — parent player to echo transform
                if (currentPlatform != groundHit.transform)
                {
                    currentPlatform  = groundHit.transform;
                    lastPlatformPos  = currentPlatform.position;
                    transform.SetParent(currentPlatform);
                }

                // Track velocity by measuring how far the platform moved this frame
                Vector3 delta    = currentPlatform.position - lastPlatformPos;
                platformVelocity = delta / Time.fixedDeltaTime;
                lastPlatformPos  = currentPlatform.position;
            }
            else
            {
                // Landed on normal ground — leave platform without inheriting velocity
                // (walking off edge shouldn't give a sudden horizontal kick)
                LeavePlatform(inherit: false);
            }
        }
        else
        {
            // Went airborne — unparent and inherit platform velocity so jump carries momentum
            LeavePlatform(inherit: true);
        }
    }

    void LeavePlatform(bool inherit)
    {
        if (currentPlatform == null) return;

        transform.SetParent(originalParent);

        if (inherit && platformVelocity.sqrMagnitude > 0.01f)
            rb.linearVelocity += platformVelocity;

        currentPlatform  = null;
        platformVelocity = Vector2.zero;
    }

    // ───────────────────────────────────────────
    // WALL JUMP
    // ───────────────────────────────────────────

    void DoWallJump()
    {
        rb.linearVelocity = new Vector2(-wallDirection * wallJumpForceX, wallJumpForceY);
        jumpBufferTimer   = 0f;
        coyoteTimer       = 0f;
        isOnWall          = false;
        isJumping         = false;
    }

    // ───────────────────────────────────────────
    // DEATH / RESPAWN
    // ───────────────────────────────────────────

    void Respawn()
    {
        if (isDead) return;
        isDead = true;

        // Unparent from any platform before death so the scene reload is clean
        if (currentPlatform != null)
        {
            transform.SetParent(originalParent);
            currentPlatform = null;
        }

        state.RaiseDeath();
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType       = RigidbodyType2D.Kinematic;

        yield return new WaitForSeconds(deathDelay);

        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
    }

    public void TriggerDeath() => Respawn();

    public void TeleportTo(Vector2 position)
    {
        // Unparent from any platform before teleporting
        if (currentPlatform != null)
        {
            transform.SetParent(originalParent);
            currentPlatform  = null;
            platformVelocity = Vector2.zero;
        }

        transform.position = position;
        rb.linearVelocity  = Vector2.zero;
        isJumping          = false;
    }

    public void SetVelocity(Vector2 vel)
    {
        rb.linearVelocity = vel;
    }

    public void SetSpawnPoint(Transform newSpawn) { }

    // ───────────────────────────────────────────
    // STATE HUB
    // ───────────────────────────────────────────

    void UpdateStateHub(bool isGrounded)
    {
        state.Position   = rb.position;
        state.Velocity   = rb.linearVelocity;
        state.Facing     = facing;
        state.IsGrounded = isGrounded;

        if (isOnWall)                                     state.AnimState = "WallSlide";
        else if (!isGrounded)                             state.AnimState = "Jump";
        else if (Mathf.Abs(rb.linearVelocity.x) > 0.1f) state.AnimState = "Run";
        else                                              state.AnimState = "Idle";
    }

    // ───────────────────────────────────────────
    // PUBLIC GETTERS
    // ───────────────────────────────────────────

    public bool IsGrounded => wasGrounded;
    public bool IsOnWall   => isOnWall;
    public int  Facing     => facing;

    // ───────────────────────────────────────────
    // GIZMOS
    // ───────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, new Vector3(GroundCheckSize.x, GroundCheckSize.y, 0f));

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.right * WallCheckOffset, WallCheckRadius);
        Gizmos.DrawWireSphere(transform.position + Vector3.left  * WallCheckOffset, WallCheckRadius);
    }
}