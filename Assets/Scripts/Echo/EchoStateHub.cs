using UnityEngine;

public enum EchoState
{
    Following,
    Frozen
}

// Shared echo state. Lives on the Echo GameObject.
// EchoController writes state, other echo components read it.
public class EchoStateHub : MonoBehaviour
{
    [Header("Runtime State (read-only in Inspector)")]
    public EchoState State = EchoState.Following;
}