using UnityEngine;

/// <summary>
/// Reads the recording buffer at a delay offset and drives the echo's
/// transform each FixedUpdate. Only active when echo is Following.
/// Lives on the Echo GameObject.
/// </summary>
public class EchoReplayer : MonoBehaviour
{
    [Header("Delay Settings")]
    [Tooltip("Echo delay in seconds. Converted to frames internally.")]
    public float echoDelaySeconds = 3f;

    [Header("References")]
    public EchoRecordingBuffer buffer;
    public EchoStateHub echoState;

    // --- Internal ---
    int delayFrames;

    /// <summary>
    /// Whether the buffer has enough frames to produce a valid replay position.
    /// EchoVisuals and EchoController can read this for the "charging" indicator.
    /// </summary>
    public bool IsBufferReady { get; private set; }

    /// <summary>
    /// The last valid frame read from the buffer. Other components can read this
    /// to know where the echo is in its replay (e.g. for freeze positioning).
    /// </summary>
    public EchoFrame LastReadFrame { get; private set; }

    void Start()
    {
        RecalculateDelayFrames();
    }

    void FixedUpdate()
    {
        // Only replay when Following — Frozen echo doesn't move
        if (echoState.State != EchoState.Following)
            return;

        RecalculateDelayFrames();

        if (buffer.TryReadAtDelay(delayFrames, out EchoFrame frame))
        {
            IsBufferReady = true;
            LastReadFrame = frame;

            transform.position = frame.position;

            // Mirror facing
            float scaleX = frame.facing;
            transform.localScale = new Vector3(scaleX, 1f, 1f);
        }
        else
        {
            IsBufferReady = false;
            // Buffer underflow — echo stays at its current position
            // (will be at player's position after a Clear, which is correct)
        }
    }

    /// <summary>
    /// Recalculate delay frames from seconds. Called each FixedUpdate
    /// so Inspector changes to echoDelaySeconds take effect immediately.
    /// </summary>
    void RecalculateDelayFrames()
    {
        delayFrames = Mathf.RoundToInt(echoDelaySeconds / Time.fixedDeltaTime);
        // Safety: clamp to buffer size
        delayFrames = Mathf.Min(delayFrames, buffer.BufferSize - 1);
    }

    public int DelayFrames => delayFrames;
}
