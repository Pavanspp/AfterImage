using UnityEngine;

public class EchoRecordingBuffer : MonoBehaviour
{
    [Header("Buffer Settings")]
    [Tooltip("Buffer size. 512 @ 50Hz FixedUpdate = ~10s max storage.")]
    public int bufferSize = 512;

    [Header("References")]
    public PlayerStateHub playerState;

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

    private void Write(EchoFrame frame)
    {
        buffer[writeHead] = frame;
        writeHead = (writeHead + 1) % bufferSize;
        frameCount++;
    }

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

    public void Clear()
    {
        writeHead = 0;
        frameCount = 0;
    }

    public int BufferSize => bufferSize;
}

public struct EchoFrame
{
    public Vector2 position;
    public Vector2 velocity;
    public int     facing;
    public string  animState;
}