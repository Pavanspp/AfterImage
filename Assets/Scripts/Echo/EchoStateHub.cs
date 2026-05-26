using UnityEngine;

public enum EchoState
{
    Following,
    Frozen
}

public class EchoStateHub : MonoBehaviour
{
    [Header("Runtime State (read-only in Inspector)")]
    public EchoState State = EchoState.Following;
}
