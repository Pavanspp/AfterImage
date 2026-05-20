using UnityEngine;

// Single source of truth for active zone modifiers.
// Lives on the Player GameObject.
// Zones set flags on enter/exit.
// EchoController reads flags at Freeze/Teleport moment.
public class ZoneStateTracker : MonoBehaviour
{
    // Momentum Zone — inside-only, checked at teleport
    public bool isInsideMomentumZone;

    // Kinetic Freeze Zone — persistent charge, consumed on freeze
    public bool hasKineticFreezeCharge;
    public bool isInsideKineticZone;

    // Echo Split Zone — persistent charge, consumed on freeze (future)
    public bool hasEchoSplitCharge;
    public bool isInsideEchoSplitZone;

    // Legacy field kept for compatibility (not actively used)
    public bool hasMomentumCharge;

    public void ClearAllCharges()
    {
        isInsideMomentumZone = false;
        hasMomentumCharge = false;
        hasKineticFreezeCharge = false;
        isInsideKineticZone = false;
        hasEchoSplitCharge = false;
        isInsideEchoSplitZone = false;
    }
}