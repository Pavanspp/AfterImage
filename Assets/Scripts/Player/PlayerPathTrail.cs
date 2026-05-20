using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Spawns fading, shrinking position stamps behind the player showing
// the path the echo is currently replaying toward.
// Lives on the Player GameObject.
//
// State-aware:
//   Following + buffer ready    → stamps spawn and age normally
//   Following + buffer charging → stamps spawn at reduced opacity
//   Frozen                      → no new stamps, all existing stamps cleared instantly
//   Death                       → all stamps cleared instantly, no new stamps until respawn
public class PlayerPathTrail : MonoBehaviour
{
    [Header("References")]
    public EchoReplayer echoReplayer;
    public EchoStateHub echoState;
    public Sprite stampSprite;
    public string sortingLayerName = "Default";
    public int sortingOrder = -1;

    PlayerStateHub playerState;

    [Header("Stamp Appearance")]
    public float spawnInterval = 0.08f;
    public float startSize = 0.35f;
    public Color stampColor = new Color(0.6f, 0.7f, 1f, 0.55f);
    public Color chargingStampColor = new Color(0.5f, 0.5f, 0.7f, 0.2f);
    public bool shrinkWithAge = true;
    public float minSizeScale = 0.05f;

    [Header("Pulse on newest stamp")]
    public bool pulseNewest = true;
    public float pulseSpeed = 4f;
    public float pulseAmount = 0.08f;

    // ── Internal ──
    float spawnTimer;
    GameObject newestStamp;
    bool isDead;

    readonly List<GameObject> liveStamps = new List<GameObject>();

    void Awake()
    {
        playerState = GetComponent<PlayerStateHub>();
    }

    void OnEnable()
    {
        playerState.OnDeath += OnDeath;
    }

    void OnDisable()
    {
        playerState.OnDeath -= OnDeath;
        ClearAllStamps();
    }

    void OnDeath()
    {
        isDead = true;
        ClearAllStamps();
    }

    public void OnRespawn()
    {
        isDead = false;
    }

    void Update()
    {
        if (isDead) return;

        if (echoState.State == EchoState.Frozen)
        {
            ClearAllStamps();
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnStamp();
            spawnTimer = spawnInterval;
        }

        if (pulseNewest && newestStamp != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            newestStamp.transform.localScale = Vector3.one * startSize * pulse;
        }
    }

    void SpawnStamp()
    {
        float lifetime = echoReplayer != null ? echoReplayer.echoDelaySeconds : 1.2f;

        Color color = (echoReplayer != null && !echoReplayer.IsBufferReady)
            ? chargingStampColor
            : stampColor;

        GameObject stamp = new GameObject("PathStamp");
        stamp.transform.position   = transform.position;
        stamp.transform.localScale = Vector3.one * startSize;

        SpriteRenderer sr   = stamp.AddComponent<SpriteRenderer>();
        sr.sprite           = stampSprite;
        sr.color            = color;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder     = sortingOrder;

        liveStamps.Add(stamp);
        newestStamp = stamp;

        StartCoroutine(AgeStamp(stamp, sr, lifetime, color));
    }

    IEnumerator AgeStamp(GameObject stamp, SpriteRenderer sr, float lifetime, Color startColor)
    {
        float fadeInDuration = 0.05f;
        float fadeInElapsed  = 0f;
        while (fadeInElapsed < fadeInDuration)
        {
            if (stamp == null) yield break;
            fadeInElapsed += Time.deltaTime;
            Color c = startColor;
            c.a = startColor.a * (fadeInElapsed / fadeInDuration);
            sr.color = c;
            yield return null;
        }

        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            if (stamp == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            float fadeT = Mathf.Clamp01((t - 0.5f) / 0.5f);
            float alpha = startColor.a * (1f - Mathf.Pow(fadeT, 2f));
            Color col   = startColor;
            col.a       = alpha;
            sr.color    = col;

            if (shrinkWithAge && stamp != newestStamp)
            {
                float shrinkT   = Mathf.Clamp01((t - 0.4f) / 0.6f);
                float sizeScale = Mathf.Lerp(1f, minSizeScale, Mathf.Pow(shrinkT, 2f));
                stamp.transform.localScale = Vector3.one * startSize * sizeScale;
            }

            yield return null;
        }

        RemoveAndDestroy(stamp);
    }

    void ClearAllStamps()
    {
        StopAllCoroutines();

        for (int i = liveStamps.Count - 1; i >= 0; i--)
        {
            if (liveStamps[i] != null)
                Destroy(liveStamps[i]);
        }
        liveStamps.Clear();
        newestStamp = null;
    }

    void RemoveAndDestroy(GameObject stamp)
    {
        liveStamps.Remove(stamp);
        if (stamp == newestStamp) newestStamp = null;
        if (stamp != null) Destroy(stamp);
    }
}