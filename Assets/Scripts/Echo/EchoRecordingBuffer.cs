using UnityEngine;

// Fixed-size circular buffer that records player state every FixedUpdate.
// O(1) read and write. Lives on the Echo GameObject.
// Reads from PlayerStateHub on the Player.
public class EchoRecordingBuffer : MonoBehaviour
{
    [Header("Buffer Settings")]
    [Tooltip("Buffer size. 512 @ 50Hz FixedUpdate = ~10s max storage.")]
    public int bufferSize = 512;

    [Header("References")]
    public PlayerStateHub playerState;

    // --- Internal state ---
    EchoFrame[] buffer;
    int writeHead = 0;
    int frameCount = 0;

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
            animState = playerState.AnimState
        });
    }

    // Write a frame to the buffer. Advances write head, wraps on overflow.
    private void Write(EchoFrame frame)
    {
        buffer[writeHead] = frame;
        writeHead = (writeHead + 1) % bufferSize;
        frameCount++;
    }

    // Read a frame at a given delay (in frames) behind the write head.
    // Returns false if the buffer doesn't have enough data yet (underflow).
    public bool TryReadAtDelay(int delayFrames, out EchoFrame frame)
    {
        if (delayFrames > bufferSize)
        {
            frame = default;
            return false;
        }

        if (frameCount < delayFrames)
        {
            frame = default;
            return false;
        }

        int index = (writeHead - delayFrames + bufferSize) % bufferSize;
        frame = buffer[index];
        return true;
    }

    // Clear the buffer. Used on unfreeze to reset echo history.
    public void Clear()
    {
        writeHead = 0;
        frameCount = 0;
    }

    public int BufferSize => bufferSize;
}

// One frame of recorded player state. Struct for value-type copies.
public struct EchoFrame
{
    public Vector2 position;
    public Vector2 velocity;
    public int     facing;     // 1 or -1
    public string  animState;  // "Run", "Jump", "Idle"
}