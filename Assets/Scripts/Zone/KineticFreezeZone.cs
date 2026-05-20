using UnityEngine;

// Kinetic Freeze Zone — while inside, freezing the echo gives it its
// replay velocity so it slides as a moving platform until hitting geometry.
// Inside-only: player must be in the zone when freeze fires.
// Riddle: "You froze it. It didn't forget."
// Color: Amber #FFBF00
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