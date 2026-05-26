using UnityEngine;

public class EchoReplayer : MonoBehaviour
{
    [Header("Delay Settings")]
    [Tooltip("Echo delay in seconds. Converted to frames internally.")]
    public float echoDelaySeconds = 1.2f;

    [Header("References")]
    public EchoRecordingBuffer buffer;
    public EchoStateHub echoState;

    int delayFrames;

    public bool IsBufferReady { get; private set; }
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

    void RecalculateDelayFrames()
    {
        delayFrames = Mathf.RoundToInt(echoDelaySeconds / Time.fixedDeltaTime);
        delayFrames = Mathf.Min(delayFrames, buffer.BufferSize - 1);
    }

    public int DelayFrames => delayFrames;
}