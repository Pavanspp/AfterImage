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
    public float moveSpeed = 12f;
    public float jumpForce = 14f;
    int facing = 1;

    [Header("Air Control")]
    public float airAcceleration = 20f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Ground Movement Decay")]
    public float momentumDecayRate = 10f;

    [Header("Jump Feel")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    [Tooltip("On early jump release, vertical velocity is multiplied by this. " +
             "0.4 = short hop is 40% of full jump height. Lower = snappier short hop.")]
    public float jumpCutMultiplier = 0.4f;

    [Header("Wall Jump")]
    public LayerMask wallLayer;
    public float wallJumpForceX = 15f;
    public float wallJumpForceY = 15f;

    [Header("Respawn")]
    public Transform spawnPoint;
    public float deathFloorY = -20f;

    // ───────────────────────────────────────────
    // PRIVATE STATE
    // ───────────────────────────────────────────
    float moveX;

    float coyoteTimer;
    float jumpBufferTimer;

    bool isOnWall;
    int  wallDirection;

    float baseGravity;
    float airSpeed;
    float preservedMax;
    bool  wasGrounded = false;

    // Tracks whether we're in an active jump that can still be cut
    bool isJumping = false;

    // Wall check constants — calibrated for 1x1 square collider
    const float WallCheckOffset = 0.55f;
    const float WallCheckRadius = 0.2f;

    // Ground check constants — box wider than circle to catch platform edges reliably
    static readonly Vector2 GroundCheckSize = new Vector2(0.8f, 0.1f);

    // ───────────────────────────────────────────
    // LIFECYCLE
    // ───────────────────────────────────────────

    void Awake()
    {
        rb    = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();
        state = GetComponent<PlayerStateHub>();

        baseGravity  = rb.gravityScale;
        preservedMax = moveSpeed;
    }

    // ---------------------------
    // UPDATE (INPUT)
    // ---------------------------
    void Update()
    {
        Vector2 move = input.Move;
        float mag  = Mathf.Clamp01(move.magnitude);
        float xDir = Mathf.Sign(move.x);
        moveX = xDir * mag;

        if (moveX != 0)
        {
            facing = moveX > 0 ? 1 : -1;
            transform.localScale = new Vector3(facing, 1f, 1f);
        }

        // JUMP BUFFER
        if (input.JumpPressedThisFrame)
        {
            jumpBufferTimer = jumpBufferTime;

            // Fresh physics queries — not stale from last FixedUpdate
            bool grounded  = Physics2D.OverlapBox(groundCheck.position, GroundCheckSize, 0f, groundLayer);
            bool wallRight = Physics2D.OverlapCircle(transform.position + Vector3.right * WallCheckOffset, WallCheckRadius, wallLayer | groundLayer);
            bool wallLeft  = Physics2D.OverlapCircle(transform.position + Vector3.left  * WallCheckOffset, WallCheckRadius, wallLayer | groundLayer);

            if (!grounded && (wallRight || wallLeft))
            {
                wallDirection = wallRight ? 1 : -1;
                DoWallJump();
            }
        }

        // VARIABLE JUMP HEIGHT — cut velocity on early release
        // Only applies while rising from a normal jump (not wall jump)
        if (input.JumpReleasedThisFrame && isJumping && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            isJumping = false;
        }

        // DEATH FLOOR
        if (transform.position.y < deathFloorY)
            Respawn();
    }

    // ---------------------------
    // FIXED UPDATE (PHYSICS)
    // ---------------------------
    void FixedUpdate()
    {
        jumpBufferTimer -= Time.fixedDeltaTime;

        // ---- GROUND CHECK ----
        bool isGrounded = Physics2D.OverlapBox(groundCheck.position, GroundCheckSize, 0f, groundLayer);

        // Clear isJumping once we land so the cut can't fire on the next jump's takeoff frame
        if (isGrounded) isJumping = false;

        // ---- WALL DETECTION ----
        bool touchingWallRight = Physics2D.OverlapCircle(
            transform.position + Vector3.right * WallCheckOffset, WallCheckRadius, wallLayer | groundLayer);
        bool touchingWallLeft = Physics2D.OverlapCircle(
            transform.position + Vector3.left  * WallCheckOffset, WallCheckRadius, wallLayer | groundLayer);

        if (!isGrounded && touchingWallRight)
        {
            isOnWall      = true;
            wallDirection = 1;
        }
        else if (!isGrounded && touchingWallLeft)
        {
            isOnWall      = true;
            wallDirection = -1;
        }
        else
        {
            isOnWall = false;
        }

        // ---- WALL JUMP — buffer fires when touching wall (pre-press case) ----
        if (isOnWall && jumpBufferTimer > 0f)
            DoWallJump();

        // ---- MOMENTUM SYSTEM ----
        float currentSpeed = Mathf.Abs(rb.linearVelocity.x);

        if (!isGrounded)
            airSpeed = rb.linearVelocity.x;

        if (isGrounded && !wasGrounded)
            preservedMax = Mathf.Max(preservedMax, Mathf.Abs(airSpeed));

        wasGrounded = isGrounded;

        if (isGrounded && currentSpeed < moveSpeed * 0.4f)
            preservedMax = Mathf.MoveTowards(preservedMax, moveSpeed, (momentumDecayRate * 4f) * Time.fixedDeltaTime);
        else if (Mathf.Abs(moveX) > 0.1f && Mathf.Sign(rb.linearVelocity.x) != Mathf.Sign(moveX))
            preservedMax = Mathf.MoveTowards(preservedMax, moveSpeed, (momentumDecayRate * 3f) * Time.fixedDeltaTime);
        else
            preservedMax = Mathf.MoveTowards(preservedMax, moveSpeed, momentumDecayRate * Time.fixedDeltaTime);

        // ---- COYOTE TIME ----
        if (isGrounded) coyoteTimer = coyoteTime;
        else            coyoteTimer -= Time.fixedDeltaTime;

        // ---- GROUND MOVEMENT ----
        if (isGrounded)
        {
            float vx       = rb.linearVelocity.x;
            float accel    = 80f;
            float brake    = 120f;
            float friction = 25f;
            float target   = moveX * preservedMax;

            if (Mathf.Abs(moveX) > 0.01f)
            {
                if (Mathf.Abs(vx) <= Mathf.Abs(preservedMax))
                    vx = Mathf.MoveTowards(vx, target, accel * Time.fixedDeltaTime);
                else if (Mathf.Sign(vx) == Mathf.Sign(moveX))
                    vx = Mathf.MoveTowards(vx, target, friction * Time.fixedDeltaTime);
                else
                    vx = Mathf.MoveTowards(vx, target, brake * Time.fixedDeltaTime);
            }
            else
            {
                vx = Mathf.MoveTowards(vx, 0f, friction * Time.fixedDeltaTime);
            }

            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }
        // ---- AIR MOVEMENT ----
        else
        {
            float vx    = rb.linearVelocity.x;
            float steer = airAcceleration * Time.fixedDeltaTime;

            if (Mathf.Abs(moveX) > 0.01f)
            {
                if (Mathf.Abs(vx) < moveSpeed)
                    vx = Mathf.MoveTowards(vx, moveX * moveSpeed, steer);
                else if (Mathf.Sign(moveX) != Mathf.Sign(vx))
                    vx = Mathf.MoveTowards(vx, moveX * moveSpeed, steer);
            }

            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }

        // ---- JUMP ----
        if (!isOnWall && jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer   = 0f;
            coyoteTimer       = 0f;
            isJumping         = true;
        }

        // ---- UPDATE STATE HUB ----
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
        isJumping         = false; // wall jumps are not cuttable
    }

    // ───────────────────────────────────────────
    // RESPAWN / TELEPORT
    // ───────────────────────────────────────────

    void Respawn()
    {
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

        if (isOnWall)                                        state.AnimState = "WallSlide";
        else if (!isGrounded)                                state.AnimState = "Jump";
        else if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)    state.AnimState = "Run";
        else                                                 state.AnimState = "Idle";
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