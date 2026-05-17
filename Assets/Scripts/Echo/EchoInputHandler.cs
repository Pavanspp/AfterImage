using UnityEngine;

// Bridges input events from PlayerInputReader to EchoController.
// Lives on the Player GameObject (same as PlayerInputReader).
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
        input.OnFreezePressed    += HandleFreeze;
        input.OnTeleportPressed  += HandleTeleport;
    }

    void OnDisable()
    {
        input.OnFreezePressed    -= HandleFreeze;
        input.OnTeleportPressed  -= HandleTeleport;
    }

    void HandleFreeze()   => echoController.ToggleFreeze();
    void HandleTeleport() => echoController.Teleport();
}