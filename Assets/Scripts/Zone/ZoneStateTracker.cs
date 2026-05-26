using UnityEngine;

public class ZoneStateTracker : MonoBehaviour
{
    public bool isInsideMomentumZone;

    public bool hasKineticFreezeCharge;
    public bool isInsideKineticZone;

    public bool hasEchoSplitCharge;
    public bool isInsideEchoSplitZone;

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