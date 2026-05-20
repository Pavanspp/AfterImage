using UnityEngine;
using System.Collections.Generic;

// Procedural electric arc effect with branching and metal emitters.
// Use Vertical toggle for vertical orientation — no rotation needed.
// Requires: BoxCollider2D (IsTrigger, Hazard layer) + Hazard.cs
public class StaticCharge : MonoBehaviour
{
    [Header("Visual")]
    public Color innerColor  = new Color(1f, 0.2f, 0.5f, 0.9f);
    public Color glowColor   = new Color(1f, 0.2f, 0.5f, 0.25f);
    public float innerWidth  = 0.03f;
    public float glowWidth   = 0.1f;

    [Header("Arc Settings")]
    public int   segmentsPerArc = 8;
    public float spreadAmount   = 0.35f;
    public float jitterInterval = 0.05f;
    public float flickerMin     = 0.3f;
    public float flickerMax     = 1.0f;

    [Header("Branching")]
    public int   branchesPerArc = 2;
    public int   branchSegments = 3;
    public float branchSpread   = 0.2f;

    [Header("Density")]
    public int boltCount = 5;

    [Header("Orientation")]
    public bool vertical = false;

    [Header("Emitters")]
    public float emitterWidth    = 0.08f;
    public float emitterHeight   = 1f;
    public Color emitterColor    = new Color(1f, 0.2f, 0.5f, 1f);
    public Color emitterDimColor = new Color(0.3f, 0.08f, 0.15f, 1f);

    [Header("State")]
    public bool startsActive = true;

    class Arc
    {
        public LineRenderer inner;
        public LineRenderer glow;
        public Vector3 start;
        public Vector3 end;
        public bool isBranch;
    }

    List<Arc>            arcs                 = new List<Arc>();
    List<SpriteRenderer> emitterCoreRenderers = new List<SpriteRenderer>();
    float         jitterTimer;
    BoxCollider2D col;
    float         left, right, bottom, top;
    bool          isActive;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();

        Vector2 size   = col.size;
        Vector2 offset = col.offset;
        left   = offset.x - size.x * 0.5f;
        right  = offset.x + size.x * 0.5f;
        bottom = offset.y - size.y * 0.5f;
        top    = offset.y + size.y * 0.5f;

        BuildAllArcs();
        BuildEmitters();

        isActive = startsActive;
        if (!isActive) SetActive(false);
    }

    // ── Arcs ────────────────────────────────────────────────────────────

    void BuildAllArcs()
    {
        foreach (var a in arcs)
        {
            if (a.inner != null) Destroy(a.inner.gameObject);
            if (a.glow  != null) Destroy(a.glow.gameObject);
        }
        arcs.Clear();

        for (int i = 0; i < boltCount; i++)
        {
            float frac = (float)i / Mathf.Max(boltCount - 1, 1);
            Vector3 start, end;

            if (vertical)
            {
                float midX = Mathf.Lerp(left + 0.15f, right - 0.15f, frac)
                           + Random.Range(-0.1f, 0.1f);
                start = LocalToWorld(midX + Random.Range(-0.05f, 0.05f), bottom);
                end   = LocalToWorld(midX + Random.Range(-0.05f, 0.05f), top);
            }
            else
            {
                float midY = Mathf.Lerp(bottom + 0.15f, top - 0.15f, frac)
                           + Random.Range(-0.1f, 0.1f);
                start = LocalToWorld(left,  midY + Random.Range(-0.05f, 0.05f));
                end   = LocalToWorld(right, midY + Random.Range(-0.05f, 0.05f));
            }

            Arc arc = BuildArc(start, end, false, i * 6);
            arcs.Add(arc);
            AddBranches(arc, i * 6);
        }

        Jitter();
    }

    Arc BuildArc(Vector3 start, Vector3 end, bool isBranch, int sortBase)
    {
        Arc arc = new Arc { start = start, end = end, isBranch = isBranch };
        arc.inner = CreateLine(isBranch ? innerWidth * 0.6f : innerWidth, innerColor, sortBase + 1);
        arc.glow  = CreateLine(isBranch ? glowWidth  * 0.5f : glowWidth,  glowColor,  sortBase);
        return arc;
    }

    void AddBranches(Arc parent, int sortBase)
    {
        for (int b = 0; b < branchesPerArc; b++)
        {
            float   t      = Random.Range(0.2f, 0.8f);
            Vector3 origin = Vector3.Lerp(parent.start, parent.end, t);
            Vector3 dir    = (parent.end - parent.start).normalized;
            Vector3 perp   = new Vector3(-dir.y, dir.x, 0f);
            float   side   = Random.value > 0.5f ? 1f : -1f;

            Vector3 branchEnd = origin
                + perp * side * Random.Range(0.15f, branchSpread * 2f)
                + dir  * Random.Range(0.05f, 0.25f);

            Vector3 local = transform.InverseTransformPoint(branchEnd);
            local.x   = Mathf.Clamp(local.x, left,   right);
            local.y   = Mathf.Clamp(local.y, bottom,  top);
            branchEnd = transform.TransformPoint(local);

            arcs.Add(BuildArc(origin, branchEnd, true, sortBase + b * 2 + 10));
        }
    }

    void Jitter()
    {
        foreach (var arc in arcs)
        {
            int   segs   = arc.isBranch ? branchSegments : segmentsPerArc;
            float spread = arc.isBranch ? branchSpread   : spreadAmount;

            Vector3[] points = new Vector3[segs + 2];
            points[0]        = arc.start;
            points[segs + 1] = arc.end;

            Vector3 dir  = (arc.end - arc.start).normalized;
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

            for (int s = 1; s <= segs; s++)
            {
                float   t      = (float)s / (segs + 1);
                Vector3 lerped = Vector3.Lerp(arc.start, arc.end, t);
                lerped += perp * Random.Range(-spread, spread);
                lerped += new Vector3(
                    Random.Range(-spread * 0.3f, spread * 0.3f),
                    Random.Range(-spread * 0.2f, spread * 0.2f), 0f);
                points[s] = lerped;
            }

            float flicker = Random.Range(flickerMin, flickerMax);
            Color ic = innerColor; ic.a *= flicker;
            Color gc = glowColor;  gc.a *= flicker;

            arc.inner.positionCount = segs + 2;
            arc.glow.positionCount  = segs + 2;
            arc.inner.SetPositions(points);
            arc.glow.SetPositions(points);
            arc.inner.startColor = ic; arc.inner.endColor = ic;
            arc.glow.startColor  = gc; arc.glow.endColor  = gc;
        }
    }

    // ── Emitters ─────────────────────────────────────────────────────────

    void BuildEmitters()
    {
        if (vertical)
        {
            BuildEmitter("EmitterBottom", bottom, false);
            BuildEmitter("EmitterTop",    top,    true);
        }
        else
        {
            BuildEmitter("EmitterLeft",  left,  false);
            BuildEmitter("EmitterRight", right, true);
        }
    }

    void BuildEmitter(string name, float localEdge, bool isSecond)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(transform);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale    = Vector3.one;
        root.transform.localPosition = vertical
            ? new Vector3(col.offset.x, localEdge, 0f)
            : new Vector3(localEdge, col.offset.y, 0f);

        // Horizontal emitter: narrow+tall. Vertical emitter: wide+short.
        float eW = vertical ? Mathf.Min(emitterHeight, col.size.x) : emitterWidth;
        float eH = vertical ? emitterWidth : Mathf.Min(emitterHeight, col.size.y);

        // Inward highlight offset toward center of field
        float inwardX = vertical ? 0f : (isSecond ? -emitterWidth * 0.4f :  emitterWidth * 0.4f);
        float inwardY = vertical ? (isSecond ? -emitterWidth * 0.4f : emitterWidth * 0.4f) : 0f;

        // Body
        CreateEmitterRect(root, "Body",
            Vector3.zero, new Vector3(eW, eH, 1f),
            new Color(0.15f, 0.08f, 0.12f, 1f), 5);

        // Core — tracked for dimming
        SpriteRenderer coreSR = CreateEmitterRect(root, "Core",
            Vector3.zero, new Vector3(eW * 0.25f, eH * 0.85f, 1f),
            new Color(emitterColor.r, emitterColor.g, emitterColor.b, 0.9f), 7);
        emitterCoreRenderers.Add(coreSR);

        // Edge highlight
        CreateEmitterRect(root, "EdgeHighlight",
            new Vector3(inwardX, inwardY, 0f),
            new Vector3(vertical ? eW : eW * 0.08f, vertical ? eH * 0.08f : eH, 1f),
            new Color(1f, 0.7f, 0.85f, 0.8f), 8);

        // Glow
        CreateEmitterRect(root, "Glow",
            Vector3.zero, new Vector3(eW * 2.5f, eH * 0.6f, 1f),
            new Color(emitterColor.r, emitterColor.g, emitterColor.b, 0.08f), 4);

        // Rivets — run along the long axis
        int rivetCount = Mathf.Max(2, Mathf.RoundToInt((vertical ? eW : eH) * 2f));
        for (int r = 0; r < rivetCount; r++)
        {
            float t       = (float)r / Mathf.Max(rivetCount - 1, 1);
            float rivetX  = vertical ? Mathf.Lerp(-eW * 0.45f, eW * 0.45f, t) : 0f;
            float rivetY  = vertical ? 0f : Mathf.Lerp(-eH * 0.45f, eH * 0.45f, t);
            float rivetSz = vertical ? eH * 0.5f : eW * 0.5f;

            CreateEmitterRect(root, "Rivet_" + r,
                new Vector3(rivetX, rivetY, 0f),
                new Vector3(rivetSz, rivetSz, 1f),
                new Color(0.9f, 0.5f, 0.7f, 1f), 9);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    SpriteRenderer CreateEmitterRect(GameObject parent, string name, Vector3 localPos, Vector3 scale, Color color, int sortOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;

        SpriteRenderer sr   = go.AddComponent<SpriteRenderer>();
        sr.sprite           = GetWhiteSprite();
        sr.color            = color;
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = sortOrder;
        return sr;
    }

    LineRenderer CreateLine(float width, Color color, int sortOrder)
    {
        GameObject go = new GameObject("Arc");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        LineRenderer lr     = go.AddComponent<LineRenderer>();
        lr.useWorldSpace    = true;
        lr.startWidth       = width;
        lr.endWidth         = width;
        lr.material         = new Material(Shader.Find("Sprites/Default"));
        lr.startColor       = color;
        lr.endColor         = color;
        lr.sortingLayerName = "Default";
        lr.sortingOrder     = sortOrder;
        return lr;
    }

    Vector3 LocalToWorld(float localX, float localY)
        => transform.TransformPoint(new Vector3(localX, localY, 0f));

    Sprite GetWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    // ── On/Off ───────────────────────────────────────────────────────────

    public void SetActive(bool active)
    {
        isActive    = active;
        col.enabled = active;

        foreach (var a in arcs)
        {
            if (a.inner != null) a.inner.gameObject.SetActive(active);
            if (a.glow  != null) a.glow.gameObject.SetActive(active);
        }

        foreach (var sr in emitterCoreRenderers)
        {
            if (sr != null)
                sr.color = active
                    ? new Color(emitterColor.r, emitterColor.g, emitterColor.b, 0.9f)
                    : emitterDimColor;
        }
    }

    public void TurnOn()  => SetActive(true);
    public void TurnOff() => SetActive(false);

    // ── Update ───────────────────────────────────────────────────────────

    void Update()
    {
        if (!isActive) return;

        jitterTimer -= Time.deltaTime;
        if (jitterTimer > 0f) return;
        jitterTimer = jitterInterval + Random.Range(-0.015f, 0.015f);

        if (Random.value < 0.15f) BuildAllArcs();
        else                      Jitter();
    }
}