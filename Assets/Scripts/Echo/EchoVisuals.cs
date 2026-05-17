using UnityEngine;
using System.Collections;

// Controls echo visual appearance based on state.
// Lives on the Echo GameObject (needs a SpriteRenderer).
//
// Following: semi-transparent, blue/purple tint, ghost trail
// Frozen: full opacity, pale blue/white, glow outline feel
public class EchoVisuals : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer echoRenderer;
    public EchoStateHub echoState;
    public EchoReplayer replayer;

    [Header("Following State")]
    public Color followingColor = new Color(0.6f, 0.7f, 1f, 0.4f);

    [Header("Frozen State")]
    public Color frozenColor = new Color(0.85f, 0.9f, 1f, 1f);

    [Header("Invalid Action Flash")]
    public Color invalidFlashColor = new Color(1f, 0.3f, 0.3f, 0.6f);
    public float invalidFlashDuration = 0.15f;

    [Header("Ghost Trail (Following)")]
    [Tooltip("Spawn fading sprite copies while Following.")]
    public bool enableGhostTrail = true;
    public float trailSpawnInterval = 0.1f;
    public float trailFadeDuration = 0.3f;
    public Color trailColor = new Color(0.5f, 0.6f, 1f, 0.25f);

    [Header("Freeze VFX")]
    [Tooltip("Brief scale punch on freeze.")]
    public float freezeScalePunch = 1.3f;
    public float freezeScaleDuration = 0.12f;

    [Header("Buffer Charging Indicator")]
    public Color chargingColor = new Color(0.4f, 0.4f, 0.6f, 0.2f);

    // --- Internal ---
    float trailTimer;
    Coroutine flashCoroutine;
    Coroutine scaleCoroutine;
    bool isFlashing;

    void Start()
    {
        SetFollowing();
    }

    void Update()
    {
        if (echoState.State == EchoState.Following)
        {
            // Show charging color if buffer isn't ready
            if (!replayer.IsBufferReady && !isFlashing)
            {
                echoRenderer.color = chargingColor;
            }
            else if (!isFlashing)
            {
                echoRenderer.color = followingColor;
            }

            // Ghost trail
            if (enableGhostTrail && replayer.IsBufferReady)
            {
                trailTimer -= Time.deltaTime;
                if (trailTimer <= 0f)
                {
                    SpawnTrailGhost();
                    trailTimer = trailSpawnInterval;
                }
            }
        }
    }

    // Set visuals to Following state.
    public void SetFollowing()
    {
        if (!isFlashing)
            echoRenderer.color = followingColor;
        trailTimer = 0f;
    }

    // Set visuals to Frozen state with scale punch.
    public void SetFrozen()
    {
        echoRenderer.color = frozenColor;

        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScalePunchRoutine());
    }

    // Brief red flash when an action is invalid (freeze-in-wall, teleport-into-wall).
    public void FlashInvalid()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(invalidFlashColor, invalidFlashDuration));
    }

    // --- Trail ---
    void SpawnTrailGhost()
    {
        GameObject ghost = new GameObject("EchoTrail");
        ghost.transform.position = transform.position;
        ghost.transform.localScale = transform.localScale;

        SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
        sr.sprite = echoRenderer.sprite;
        sr.color = trailColor;
        sr.sortingLayerName = echoRenderer.sortingLayerName;
        sr.sortingOrder = echoRenderer.sortingOrder - 1;

        StartCoroutine(FadeAndDestroy(ghost, sr, trailFadeDuration));
    }

    IEnumerator FadeAndDestroy(GameObject obj, SpriteRenderer sr, float duration)
    {
        float elapsed = 0f;
        Color startColor = sr.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            sr.color = c;
            yield return null;
        }
        Destroy(obj);
    }

    // --- Flash ---
    IEnumerator FlashRoutine(Color flashColor, float duration)
    {
        isFlashing = true;
        Color restoreColor = echoState.State == EchoState.Frozen ? frozenColor : followingColor;
        echoRenderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        echoRenderer.color = restoreColor;
        isFlashing = false;
    }

    // --- Scale punch ---
    IEnumerator ScalePunchRoutine()
    {
        Vector3 originalScale = new Vector3(
            Mathf.Sign(transform.localScale.x),
            1f, 1f);
        Vector3 punchedScale = originalScale * freezeScalePunch;

        float elapsed = 0f;
        float half = freezeScaleDuration * 0.5f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            transform.localScale = Vector3.Lerp(originalScale, punchedScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            transform.localScale = Vector3.Lerp(punchedScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}