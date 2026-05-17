using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInputReader : MonoBehaviour
{
    Controls controls;

    public Vector2 Move { get; private set; }

    public event Action OnJumpPressed;
    public event Action OnFreezePressed;
    public event Action OnTeleportPressed;

    public bool JumpPressedThisFrame     { get; private set; }
    public bool JumpReleasedThisFrame    { get; private set; }
    public bool FreezePressedThisFrame   { get; private set; }
    public bool TeleportPressedThisFrame { get; private set; }

    void Awake()
    {
        controls = new Controls();
    }

    void OnEnable()  => controls.Enable();
    void OnDisable() => controls.Disable();

    void Update()
    {
        Move = controls.Player.Move.ReadValue<Vector2>();

        JumpPressedThisFrame     = controls.Player.Jump.WasPressedThisFrame();
        JumpReleasedThisFrame    = controls.Player.Jump.WasReleasedThisFrame();
        FreezePressedThisFrame   = controls.Player.Freeze.WasPressedThisFrame();
        TeleportPressedThisFrame = controls.Player.Swap.WasPressedThisFrame();

        if (JumpPressedThisFrame)     OnJumpPressed?.Invoke();
        if (FreezePressedThisFrame)   OnFreezePressed?.Invoke();
        if (TeleportPressedThisFrame) OnTeleportPressed?.Invoke();
    }
}