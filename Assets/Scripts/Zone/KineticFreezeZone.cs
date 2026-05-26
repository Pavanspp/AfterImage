using UnityEngine;

public class KineticFreezeZone : ZoneBase
{
    ZoneBoundaryVisual visual;

    protected override void OnPlayerEnter()
    {
        tracker.isInsideKineticZone = true;

        if (visual == null) visual = GetComponent<ZoneBoundaryVisual>();
        if (visual != null)
        {
            visual.TriggerScreenFlash();
            GameObject player = GameObject.FindWithTag(playerTag);
            if (player != null)
                visual.TriggerRiddle(player.transform);
        }
    }

    protected override void OnPlayerExit()
    {
        tracker.isInsideKineticZone = false;
    }
}