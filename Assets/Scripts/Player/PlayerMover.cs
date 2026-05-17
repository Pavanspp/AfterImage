using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    // ───────────────────────────────────────────
    // REFERENCES
    // ───────────────────────────────────────────
    Rigidbody2D rb;
    PlayerInputReader input;
    PlayerStateHub state;

    // ───────────────────────────────────────────
    // INSPECTOR FIELDS
    // ───────────────────────────────────────────

    [Header("Movement")]
    public float moveSpeed    = 10f;
    public float acceleration = 60f;   // how fast we reach moveSpeed on ground
    public float deceleration = 80f;   // how fast we stop on ground when no input
    public float airAcceleration = 30f; // steering force in air — less than ground
    int facing = 1;

    [Header("Jump Feel")]
    public float jumpForce        = 15f;
    public float coyoteTime       = 0.1f;
    public float jumpBufferTime   = 0.1f;
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
    public Transform spawnPoint;
    public float deathFloorY = -20f;

    // ───────────────────────────────────────────
    // PRIVATE STATE
    // ───────────────────────────────────────────
    float moveX;
    float coyoteTimer;
    float jumpBufferTimer;
    bool  isOnWall;
    int   wallDirection;
    bool  wasGrounded;
    bool  isJumping;
    float baseGravity;

    const float WallCheckOffset = 0.55f;
    const float WallCheckRadius = 0.2f;
    static readonly Vector2 GroundCheckSize = new Vector2(0.8f, 0.1f);

    // ───────────────────────────────────────────
    // LIFECYCLE
    // ───────────────────────────────────────────

    void Awake()
    {
        rb          = GetComponent<Rigidbody2D>();
        input       = GetComponent<PlayerInputReader>();
        state       = GetComponent<PlayerStateHub>();
        baseGravity = rb.gravityScale;
    }

    // ───────────────────────────────────────────
    // UPDATE — input reads, jump cut, death floor
    // ───────────────────────────────────────────

    void Update()
    {
        // Horizontal input — binary left/right, no analog ramping
        float rawX = input.Move.x;
        moveX = rawX > 0.01f ? 1f : rawX < -0.01f ? -1f : 0f;

        if (moveX != 0)
        {
            facing = moveX > 0 ? 1 : -1;
            transform.localScale = new Vector3(facing, 1f, 1f);
        }

        // Jump buffer + immediate wall jump check on press
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

        // Variable jump height — cut on early release
        if (input.JumpReleasedThisFrame && isJumping && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            isJumping = false;
        }

        // Death floor
        if (transform.position.y < deathFloorY)
            Respawn();
    }

    // ───────────────────────────────────────────
    // FIXED UPDATE — physics
    // ───────────────────────────────────────────

    void FixedUpdate()
    {
        jumpBufferTimer -= Time.fixedDeltaTime;

        // ── Ground check ──
        bool isGrounded = Physics2D.OverlapBox(groundCheck.position, GroundCheckSize, 0f, groundLayer);
        if (isGrounded) isJumping = false;

        // ── Wall detection ──
        bool wallRight = Physics2D.OverlapCircle(transform.position + Vector3.right * WallCheckOffset, WallCheckRadius, wallLayer | groundLayer);
        bool wallLeft  = Physics2D.OverlapCircle(transform.position + Vector3.left  * WallCheckOffset, WallCheckRadius, wallLayer | groundLayer);

        if      (!isGrounded && wallRight) { isOnWall = true;  wallDirection =  1; }
        else if (!isGrounded && wallLeft)  { isOnWall = true;  wallDirection = -1; }
        else                               { isOnWall = false;                     }

        // Wall jump buffer — pre-press before touching wall
        if (isOnWall && jumpBufferTimer > 0f)
            DoWallJump();

        // ── Coyote time ──
        if (isGrounded) coyoteTimer = coyoteTime;
        else            coyoteTimer -= Time.fixedDeltaTime;

        wasGrounded = isGrounded;

        // ── Horizontal movement ──
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

        // ── Jump ──
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
    // WALL JUMP
    // ───────────────────────────────────────────

    void DoWallJump()
    {
        rb.linearVelocity = new Vector2(-wallDirection * wallJumpForceX, wallJumpForceY);
        jumpBufferTimer   = 0f;
        coyoteTimer       = 0f;
        isOnWall          = false;
        isJumping         = false; // wall jumps not cuttable
    }

    // ───────────────────────────────────────────
    // RESPAWN / TELEPORT
    // ───────────────────────────────────────────

    void Respawn()
    {
        state.RaiseDeath();              // notify all listeners first
        transform.position = spawnPoint.position;
        rb.linearVelocity  = Vector2.zero;
        isOnWall           = false;
        isJumping          = false;
        rb.gravityScale    = baseGravity;
    }

    public void TeleportTo(Vector2 position)
    {       
        transform.position = position;
        rb.linearVelocity  = Vector2.zero;
        isJumping          = false;
    }

    public void SetSpawnPoint(Transform newSpawn) { spawnPoint = newSpawn; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Hazard"))
            Respawn();
    }

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