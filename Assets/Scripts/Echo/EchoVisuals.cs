using UnityEngine;
using System.Collections;

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
    public bool enableGhostTrail = true;
    public float trailSpawnInterval = 0.1f;
    public float trailFadeDuration = 0.3f;
    public Color trailColor = new Color(0.5f, 0.6f, 1f, 0.25f);

    [Header("Freeze VFX")]
    public float freezeScalePunch = 1.3f;
    public float freezeScaleDuration = 0.12f;

    [Header("Unfreeze VFX")]
    public Color unfreezeColor = new Color(0.6f, 0.75f, 1f, 0.8f);
    public int   unfreezeFragCount = 8;
    public float unfreezeFragSpeed = 4f;
    public float unfreezeFragDuration = 0.35f;
    public float unfreezeFragSize = 0.15f;

    [Header("Buffer Ready Fade-In")]
    public float fadeInDuration = 0.3f;

    [Header("Buffer Charging Indicator")]
    public Color chargingColor = new Color(0.4f, 0.4f, 0.6f, 0.2f);

    // Cached at Start for safe VFX spawning
    Sprite cachedSprite;
    string cachedSortingLayer;
    int    cachedSortingOrder;

    float trailTimer;
    Coroutine flashCoroutine;
    Coroutine scaleCoroutine;
    bool isFlashing;

    // Fade-in state — prevents the "blink at spawn point" when buffer first fills
    bool hasEverBeenReady;
    bool isFadingIn;
    float fadeInTimer;

    void Start()
    {
        if (echoRenderer != null)
        {
            cachedSprite       = echoRenderer.sprite;
            cachedSortingLayer = echoRenderer.sortingLayerName;
            cachedSortingOrder = echoRenderer.sortingOrder;
        }

        hasEverBeenReady = false;
        isFadingIn       = false;

        // Start fully invisible — not even charging color
        echoRenderer.color = new Color(chargingColor.r, chargingColor.g, chargingColor.b, 0f);
    }

    void Update()
    {
        if (echoState.State == EchoState.Following)
        {
            if (!replayer.IsBufferReady)
            {
                // Buffer charging — stay invisible until ready
                if (!isFlashing && !isFadingIn)
                    echoRenderer.color = new Color(chargingColor.r, chargingColor.g, chargingColor.b, 0f);
            }
            else if (!hasEverBeenReady)
            {
                // Buffer JUST became ready for the first time — start fade-in
                hasEverBeenReady = true;
                isFadingIn       = true;
                fadeInTimer      = 0f;
            }

            // Smooth fade-in from invisible to followingColor
            if (isFadingIn)
            {
                fadeInTimer += Time.deltaTime;
                float t = Mathf.Clamp01(fadeInTimer / fadeInDuration);
                Color c = followingColor;
                c.a = Mathf.Lerp(0f, followingColor.a, t);
                echoRenderer.color = c;

                if (t >= 1f)
                    isFadingIn = false;
            }
            else if (hasEverBeenReady && replayer.IsBufferReady && !isFlashing)
            {
                echoRenderer.color = followingColor;
            }

            // Ghost trail — only after fully visible
            if (enableGhostTrail && hasEverBeenReady && replayer.IsBufferReady && !isFadingIn)
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

    public void SetFollowing()
    {
        if (!isFlashing)
            echoRenderer.color = followingColor;
        trailTimer = 0f;

    }

    public void SetFrozen()
    {
        echoRenderer.color = frozenColor;
        isFadingIn = false; // cancel any in-progress fade
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScalePunchRoutine());
    }

    public void FlashInvalid()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(invalidFlashColor, invalidFlashDuration));
    }

    public void SetHidden(bool hidden)
    {
        echoRenderer.enabled = !hidden;
    }

    public void PlayUnfreeze(Vector3 worldPos)
    {
        if (cachedSprite == null) return;

        StartCoroutine(UnfreezeGhostRoutine(worldPos));

        for (int i = 0; i < unfreezeFragCount; i++)
        {
            float angle = (360f / unfreezeFragCount) * i + Random.Range(-20f, 20f);
            float rad   = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 vel = dir * unfreezeFragSpeed * Random.Range(0.5f, 1.2f);
            StartCoroutine(UnfreezeFragRoutine(worldPos, vel));
        }
    }

    IEnumerator UnfreezeGhostRoutine(Vector3 pos)
    {
        GameObject ghost = new GameObject("UnfreezeGhost");
        ghost.transform.position   = pos;
        ghost.transform.localScale = Vector3.one;

        SpriteRenderer sr   = ghost.AddComponent<SpriteRenderer>();
        sr.sprite           = cachedSprite;
        sr.color            = unfreezeColor;
        sr.sortingLayerName = cachedSortingLayer;
        sr.sortingOrder     = cachedSortingOrder + 1;

        float duration = 0.25f;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            if (ghost == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float fadeT = Mathf.Clamp01((t - 0.1f) / 0.9f);
            Color c = unfreezeColor;
            c.a      = Mathf.Lerp(unfreezeColor.a, 0f, Mathf.Pow(fadeT, 1.5f));
            sr.color = c;

            float scale = Mathf.Lerp(1f, 1.25f, Mathf.Pow(t, 2f));
            ghost.transform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        if (ghost != null) Destroy(ghost);
    }

    IEnumerator UnfreezeFragRoutine(Vector3 startPos, Vector2 velocity)
    {
        GameObject frag = new GameObject("UnfreezeFrag");
        frag.transform.position = startPos;

        float size = unfreezeFragSize * Random.Range(0.6f, 1.4f);
        frag.transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer sr   = frag.AddComponent<SpriteRenderer>();
        sr.sprite           = cachedSprite;
        sr.color            = unfreezeColor;
        sr.sortingLayerName = cachedSortingLayer;
        sr.sortingOrder     = cachedSortingOrder + 1;

        float elapsed   = 0f;
        float duration  = unfreezeFragDuration * Random.Range(0.7f, 1.3f);
        float startSize = size;

        while (elapsed < duration)
        {
            if (frag == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            frag.transform.position += (Vector3)(velocity * Time.deltaTime * (1f - t * 0.7f));

            float shrinkT     = Mathf.Clamp01((t - 0.3f) / 0.7f);
            float currentSize = Mathf.Max(0.001f, Mathf.Lerp(startSize, 0f, Mathf.Pow(shrinkT, 1.5f)));
            frag.transform.localScale = new Vector3(currentSize, currentSize, 1f);

            Color c = unfreezeColor;
            c.a      = Mathf.Lerp(unfreezeColor.a, 0f, Mathf.Pow(t, 1.5f));
            sr.color = c;

            yield return null;
        }

        if (frag != null) Destroy(frag);
    }

    void SpawnTrailGhost()
    {
        GameObject ghost = new GameObject("EchoTrail");
        ghost.transform.position   = transform.position;
        ghost.transform.localScale = new Vector3(1f, 1f, 1f);

        SpriteRenderer sr   = ghost.AddComponent<SpriteRenderer>();
        sr.sprite           = echoRenderer.sprite;
        sr.color            = trailColor;
        sr.sortingLayerName = echoRenderer.sortingLayerName;
        sr.sortingOrder     = echoRenderer.sortingOrder - 1;

        StartCoroutine(FadeAndDestroy(ghost, sr, trailFadeDuration));
    }

    IEnumerator FadeAndDestroy(GameObject obj, SpriteRenderer sr, float duration)
    {
        float elapsed    = 0f;
        Color startColor = sr.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c = startColor;
            c.a      = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            sr.color = c;
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    IEnumerator FlashRoutine(Color flashColor, float duration)
    {
        isFlashing = true;
        Color restoreColor = echoState.State == EchoState.Frozen ? frozenColor : followingColor;
        echoRenderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        echoRenderer.color = restoreColor;
        isFlashing = false;
    }

    IEnumerator ScalePunchRoutine()
    {
        Vector3 original = new Vector3(1f, 1f, 1f);
        Vector3 punched  = original * freezeScalePunch;

        float elapsed = 0f;
        float half    = freezeScaleDuration * 0.5f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(original, punched, elapsed / half);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(punched, original, elapsed / half);
            yield return null;
        }

        transform.localScale = original;
    }
}