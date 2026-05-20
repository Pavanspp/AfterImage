using UnityEngine;

// Momentum Zone — modifies teleport to redirect velocity while inside.
// Only active while player is physically inside the zone.
public class MomentumZone : ZoneBase
{
    ZoneBoundaryVisual visual;

    protected override void OnPlayerEnter()
    {
        tracker.isInsideMomentumZone = true;

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
        tracker.isInsideMomentumZone = false;
    }
}