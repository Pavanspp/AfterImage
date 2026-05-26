using UnityEngine;

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