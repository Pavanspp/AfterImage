using UnityEngine;

// Reads the recording buffer at a delay offset and drives the echo's
// transform each FixedUpdate. Only active when echo is Following.
// Lives on the Echo GameObject.
public class EchoReplayer : MonoBehaviour
{
    [Header("Delay Settings")]
    [Tooltip("Echo delay in seconds. Converted to frames internally.")]
    public float echoDelaySeconds = 1.2f;

    [Header("References")]
    public EchoRecordingBuffer buffer;
    public EchoStateHub echoState;

    // --- Internal ---
    int delayFrames;

    // Whether the buffer has enough frames to produce a valid replay position.
    public bool IsBufferReady { get; private set; }

    // The last valid frame read from the buffer.
    public EchoFrame LastReadFrame { get; private set; }

    void Start()
    {
        RecalculateDelayFrames();
    }

    void FixedUpdate()
    {
        if (echoState.State != EchoState.Following)
            return;

        if (buffer.TryReadAtDelay(delayFrames, out EchoFrame frame))
        {
            IsBufferReady = true;
            LastReadFrame = frame;

            transform.position = frame.position;

            float scaleX = frame.facing;
            transform.localScale = new Vector3(scaleX, 1f, 1f);
        }
        else
        {
            IsBufferReady = false;
        }
    }

    // Convert echoDelaySeconds to frame count. Called once in Start.
    void RecalculateDelayFrames()
    {
        delayFrames = Mathf.RoundToInt(echoDelaySeconds / Time.fixedDeltaTime);
        delayFrames = Mathf.Min(delayFrames, buffer.BufferSize - 1);
    }

    public int DelayFrames => delayFrames;
}