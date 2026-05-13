using UnityEngine;

/// <summary>
/// Fixed-size circular buffer that records player state every FixedUpdate.
/// Zero GC allocations per frame. O(1) read and write.
/// Lives on the Echo GameObject. Reads from PlayerStateHub on the Player.
/// </summary>
public class EchoRecordingBuffer : MonoBehaviour
{
    [Header("Buffer Settings")]
    [Tooltip("Buffer size. 512 @ 50Hz FixedUpdate = ~10s max storage.")]
    public int bufferSize = 512;

    [Header("References")]
    [Tooltip("Drag the Player GameObject's PlayerStateHub here.")]
    public PlayerStateHub playerState;

    // --- Internal state ---
    EchoFrame[] buffer;
    int writeHead = 0;
    int frameCount = 0; // total frames written (for underflow guard)

    void Awake()
    {
        buffer = new EchoFrame[bufferSize];
    }

    void FixedUpdate()
    {
        Write(new EchoFrame
        {
            position  = playerState.Position,
            velocity  = playerState.Velocity,
            facing    = playerState.Facing,
            animState = playerState.AnimState,
            timestamp = Time.fixedTime
        });
    }

    /// <summary>
    /// Write a frame to the buffer. Advances write head, wraps on overflow.
    /// </summary>
    public void Write(EchoFrame frame)
    {
        buffer[writeHead] = frame;
        writeHead = (writeHead + 1) % bufferSize;
        frameCount++;
    }

    /// <summary>
    /// Read a frame at a given delay (in frames) behind the write head.
    /// Returns false if the buffer doesn't have enough data yet (underflow).
    /// </summary>
    public bool TryReadAtDelay(int delayFrames, out EchoFrame frame)
    {
        if (frameCount < delayFrames)
        {
            // Buffer hasn't filled enough yet — underflow
            frame = default;
            return false;
        }

        int index = (writeHead - delayFrames + bufferSize) % bufferSize;
        // Guard: if delayFrames > bufferSize, we'd be reading stale wrapped data
        if (delayFrames > bufferSize)
        {
            frame = default;
            return false;
        }

        frame = buffer[index];
        return true;
    }

    /// <summary>
    /// Read the most recently written frame (no delay).
    /// Returns false if no frames have been written.
    /// </summary>
    public bool TryReadLatest(out EchoFrame frame)
    {
        if (frameCount == 0)
        {
            frame = default;
            return false;
        }

        int index = (writeHead - 1 + bufferSize) % bufferSize;
        frame = buffer[index];
        return true;
    }

    /// <summary>
    /// Clear the buffer. Used on unfreeze and swap to reset echo history.
    /// </summary>
    public void Clear()
    {
        writeHead = 0;
        frameCount = 0;
        // No need to zero the array — frameCount guards reads
    }

    // --- Public accessors for debugging ---
    public int WriteHead => writeHead;
    public int FrameCount => frameCount;
    public int BufferSize => bufferSize;
    public bool HasEnoughFrames(int delayFrames) => frameCount >= delayFrames;
}

/// <summary>
/// One frame of recorded player state. Struct for zero GC.
/// </summary>
public struct EchoFrame
{
    public Vector2 position;
    public Vector2 velocity;
    public int     facing;     // 1 or -1
    public string  animState;  // "Run", "Jump", "Idle"
    public float   timestamp;
}
