using UnityEngine;
using System.Collections;

// Teleport visual effects. Lives on the Player GameObject.
//
// All SpriteRenderer-based. Zero LineRenderers.
// All t values clamped before Mathf.Pow to prevent NaN.
//
// Design:
//   Departure: afterimage ghost + expanding ring
//   Path: thin streak + tractor beam rings (ellipses squished along beam axis)
//   Arrival: two concentric expanding rings + scale punch
public class TeleportVisuals : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer playerRenderer;

    [Header("Afterimage")]
    public Color afterimageColor    = new Color(0.6f, 0.7f, 1f, 0.8f);
    public float afterimageDuration = 0.4f;

    [Header("Streak")]
    public Color streakColor     = new Color(0.7f, 0.85f, 1f, 0.45f);
    public float streakDuration  = 0.25f;
    public float streakThickness = 0.035f;

    [Header("Tractor Beam Rings")]
    public Color beamRingColor      = new Color(0.6f, 0.8f, 1f, 0.5f);
    public int   beamRingCount      = 5;
    public float beamRingSize       = 0.7f;
    public float beamRingDuration   = 0.3f;
    public float beamRingDepthRatio = 0.3f;

    [Header("Departure Ring")]
    public Color departureRingColor    = new Color(0.6f, 0.7f, 1f, 0.45f);
    public float departureRingSize     = 0.5f;
    public float departureRingDuration = 0.2f;

    [Header("Arrival Rings")]
    public Color innerRingColor    = new Color(0.85f, 0.92f, 1f, 0.85f);
    public Color outerRingColor    = new Color(0.6f, 0.7f, 1f, 0.3f);
    public float innerRingSize     = 0.9f;
    public float outerRingSize     = 1.4f;
    public float innerRingDuration = 0.3f;
    public float outerRingDuration = 0.45f;

    [Header("Scale Punch")]
    public float punchScale    = 1.3f;
    public float punchDuration = 0.1f;

    // Runtime sprites
    Sprite ringSprite;
    Sprite pixelSprite;

    void Awake()
    {
        // 128px ring, thin band (0.42 to 0.5 = ~8% of radius as thickness)
        // This reads as a clean line at game scale, not a chunky donut.
        ringSprite  = CreateRingSprite(128, 0.42f, 0.5f);
        pixelSprite = CreatePixelSprite();
    }

    // ───────────────────────────────────────────
    // ENTRY POINT
    // ───────────────────────────────────────────

    public void PlayTeleport(Vector3 from, Vector3 to)
    {
        // Departure
        StartCoroutine(AfterimageRoutine(from));
        StartCoroutine(FlatRingRoutine(from, departureRingColor,
            departureRingSize, departureRingDuration));

        // Path
        StartCoroutine(StreakRoutine(from, to));
        if ((to - from).sqrMagnitude > 0.01f)
            StartCoroutine(BeamRingsRoutine(from, to));

        // Arrival
        StartCoroutine(FlatRingRoutine(to, innerRingColor,
            innerRingSize, innerRingDuration));
        StartCoroutine(FlatRingRoutine(to, outerRingColor,
            outerRingSize, outerRingDuration));
        StartCoroutine(ArrivalPunchRoutine());
    }

    // ───────────────────────────────────────────
    // AFTERIMAGE — departure ghost
    // ───────────────────────────────────────────

    IEnumerator AfterimageRoutine(Vector3 position)
    {
        GameObject ghost = new GameObject("TeleportGhost");
        ghost.transform.position   = position;
        ghost.transform.localScale = Vector3.one;

        SpriteRenderer sr   = ghost.AddComponent<SpriteRenderer>();
        sr.sprite           = playerRenderer.sprite;
        sr.color            = afterimageColor;
        sr.sortingLayerName = playerRenderer.sortingLayerName;
        sr.sortingOrder     = playerRenderer.sortingOrder - 1;

        float elapsed = 0f;
        while (elapsed < afterimageDuration)
        {
            if (ghost == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / afterimageDuration);

            // Hold bright briefly, then smooth fade
            float fadeT = Mathf.Clamp01((t - 0.1f) / 0.9f);
            Color c = afterimageColor;
            c.a      = Mathf.Lerp(afterimageColor.a, 0f, Mathf.Pow(fadeT, 2f));
            sr.color = c;

            float scale = Mathf.Lerp(1f, 1.12f, Mathf.Pow(t, 2f));
            ghost.transform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        if (ghost != null) Destroy(ghost);
    }

    // ───────────────────────────────────────────
    // STREAK — thin connecting line
    // ───────────────────────────────────────────

    IEnumerator StreakRoutine(Vector3 from, Vector3 to)
    {
        Vector3 diff = to - from;
        float dist = diff.magnitude;
        if (dist < 0.01f) yield break;

        Vector3 mid = (from + to) * 0.5f;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        GameObject go = new GameObject("TeleportStreak");
        go.transform.position   = mid;
        go.transform.rotation   = Quaternion.Euler(0f, 0f, angle);
        go.transform.localScale = new Vector3(dist, streakThickness, 1f);

        SpriteRenderer sr   = go.AddComponent<SpriteRenderer>();
        sr.sprite           = pixelSprite;
        sr.color            = streakColor;
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = 5;

        float elapsed = 0f;
        while (elapsed < streakDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / streakDuration);

            // Fade and thin
            Color c = streakColor;
            c.a      = Mathf.Lerp(streakColor.a, 0f, Mathf.Pow(t, 1.5f));
            sr.color = c;

            float thick = Mathf.Lerp(streakThickness, 0.003f, t);
            go.transform.localScale = new Vector3(dist, thick, 1f);

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ───────────────────────────────────────────
    // BEAM RINGS — tractor beam ellipses along path
    // ───────────────────────────────────────────

    IEnumerator BeamRingsRoutine(Vector3 from, Vector3 to)
    {
        Vector3 diff = to - from;
        float beamAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        // Spawn all rings simultaneously across the path — no stagger
        for (int i = 0; i < beamRingCount; i++)
        {
            float lerp = (float)(i + 1) / (beamRingCount + 1);
            StartCoroutine(BeamEllipseRoutine(Vector3.Lerp(from, to, lerp), beamAngle));
        }

        yield break;
    }

    // A single tractor beam ring — an ellipse perpendicular to the beam.
    // Implemented as a ring sprite with non-uniform scale:
    //   - Full size perpendicular to beam direction
    //   - Compressed along beam direction by beamRingDepthRatio
    // Rotated to match beam angle. Gives the 3D tube cross-section illusion.
    IEnumerator BeamEllipseRoutine(Vector3 center, float beamAngleDeg)
    {
        GameObject go = new GameObject("BeamRing");
        go.transform.position = center;

        // Rotate ring so its "thin" axis aligns with the beam direction.
        // The ring sprite is circular — squishing Y after rotation makes
        // it read as a foreshortened ellipse perpendicular to the beam.
        go.transform.rotation = Quaternion.Euler(0f, 0f, beamAngleDeg);
        go.transform.localScale = new Vector3(0.05f * beamRingDepthRatio, 0.05f, 1f);

        SpriteRenderer sr   = go.AddComponent<SpriteRenderer>();
        sr.sprite           = ringSprite;
        sr.color            = beamRingColor;
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = 4;

        float elapsed = 0f;
        while (elapsed < beamRingDuration)
        {
            if (go == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / beamRingDuration);

            // Expand with ease-out
            float easedT = 1f - Mathf.Pow(Mathf.Clamp01(1f - t), 2.5f);
            float size   = Mathf.Lerp(0.05f, beamRingSize, easedT);

            // X (along beam) is compressed, Y (perpendicular) is full size
            go.transform.localScale = new Vector3(
                size * beamRingDepthRatio,
                size,
                1f);

            // Fade
            float fadeT = Mathf.Clamp01((t - 0.1f) / 0.9f);
            Color c = beamRingColor;
            c.a      = Mathf.Lerp(beamRingColor.a, 0f, Mathf.Pow(fadeT, 1.8f));
            sr.color = c;

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ───────────────────────────────────────────
    // FLAT RING — screen-space expanding circle (departure/arrival)
    // ───────────────────────────────────────────

    IEnumerator FlatRingRoutine(Vector3 center, Color ringColor,
        float targetSize, float duration)
    {
        GameObject go = new GameObject("Ring");
        go.transform.position   = center;
        go.transform.localScale = new Vector3(0.05f, 0.05f, 1f);

        SpriteRenderer sr   = go.AddComponent<SpriteRenderer>();
        sr.sprite           = ringSprite;
        sr.color            = ringColor;
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = 4;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (go == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float easedT = 1f - Mathf.Pow(Mathf.Clamp01(1f - t), 2.5f);
            float size   = Mathf.Lerp(0.05f, targetSize, easedT);
            go.transform.localScale = new Vector3(size, size, 1f);

            float fadeT = Mathf.Clamp01((t - 0.15f) / 0.85f);
            Color c = ringColor;
            c.a      = Mathf.Lerp(ringColor.a, 0f, Mathf.Pow(fadeT, 1.8f));
            sr.color = c;

            yield return null;
        }

        if (go != null) Destroy(go);
    }

    // ───────────────────────────────────────────
    // SCALE PUNCH
    // ───────────────────────────────────────────

    IEnumerator ArrivalPunchRoutine()
    {
        int     facing    = transform.localScale.x >= 0 ? 1 : -1;
        Vector3 baseScale = new Vector3(facing, 1f, 1f);
        Vector3 punched   = new Vector3(facing * punchScale, punchScale, 1f);
        float   half      = punchDuration * 0.5f;
        float   elapsed   = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            transform.localScale = Vector3.Lerp(baseScale, punched, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            transform.localScale = Vector3.Lerp(punched, baseScale, t);
            yield return null;
        }

        transform.localScale = baseScale;
    }

    // ───────────────────────────────────────────
    // RUNTIME SPRITE GENERATION
    // ───────────────────────────────────────────

    Sprite CreateRingSprite(int size, float innerRadius, float outerRadius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float center = size * 0.5f;
        Color clear  = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx   = (x - center) / center;
                float dy   = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist >= innerRadius && dist <= outerRadius)
                {
                    // Smooth edges using smoothstep-like falloff
                    float ringCenter = (innerRadius + outerRadius) * 0.5f;
                    float ringHalf   = (outerRadius - innerRadius) * 0.5f;
                    float fromCenter = Mathf.Abs(dist - ringCenter) / ringHalf;
                    float alpha      = Mathf.Clamp01(1f - fromCenter * fromCenter);

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size);
    }

    Sprite CreatePixelSprite()
    {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f), 4);
    }
}