using UnityEngine;

/// <summary>
/// Bridges input events from PlayerInputReader to EchoController.
/// Lives on the Player GameObject (same as PlayerInputReader).
/// </summary>
public class EchoInputHandler : MonoBehaviour
{
    [Header("References")]
    public EchoController echoController;

    PlayerInputReader input;

    void Awake()
    {
        input = GetComponent<PlayerInputReader>();
    }

    void OnEnable()
    {
        input.OnFreezePressed += HandleFreeze;
        input.OnSwapPressed   += HandleSwap;
    }

    void OnDisable()
    {
        input.OnFreezePressed -= HandleFreeze;
        input.OnSwapPressed   -= HandleSwap;
    }

    void HandleFreeze() => echoController.ToggleFreeze();
    void HandleSwap()   => echoController.Swap();
}