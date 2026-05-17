using UnityEngine;

// Shared player state. Lives on the Player GameObject.
// Other systems (EchoRecordingBuffer, EchoReplayer, etc.) read from this
// without needing a reference to PlayerMover internals.
// Updated by PlayerMover each FixedUpdate.
public class PlayerStateHub : MonoBehaviour
{
    public Vector2 Position   { get; set; }
    public Vector2 Velocity   { get; set; }
    public int     Facing     { get; set; } = 1;   // 1 = right, -1 = left
    public bool    IsGrounded { get; set; }
    public string  AnimState  { get; set; } = "Idle"; // "Idle", "Run", "Jump", "WallSlide"
    public event System.Action OnDeath;
    public void RaiseDeath() => OnDeath?.Invoke();
}