using UnityEngine;

// Sweeping laser hazard with BoxCast-based length — stops on terrain.
// Uses a thick BoxCast (outerWidth) so the laser can't clip past the
// edge of a blocker (frozen echo) by floating point margins.
public class TraceLaser : MonoBehaviour
{
    public enum FireDirection { Left, Right, Up, Down }

    [Header("Direction")]
    public FireDirection fireDirection = FireDirection.Right;

    [Header("Sweep")]
    public float sweepDistance = 5f;
    public float sweepSpeed    = 2f;

    [Header("Laser Visual")]
    public float innerWidth  = 0.05f;
    public float outerWidth  = 0.25f;
    public Color innerColor  = new Color(1f, 1f, 0.9f, 1f);
    public Color outerColor  = new Color(1f, 0.15f, 0.1f, 0.6f);

    [Header("Collision")]
    public LayerMask solidLayers;
    public float maxLaserLength = 50f;

    [Header("Emitter")]
    public float emitterSize   = 0.3f;
    public Color emitterColor  = new Color(1f, 0.3f, 0.2f, 1f);
    public Color emitterDimColor = new Color(0.3f, 0.08f, 0.08f, 1f);

    [Header("State")]
    public bool startsActive = true;

    LineRenderer innerLine;
    LineRenderer outerLine;
    BoxCollider2D laserCollider;
    SpriteRenderer emitterCoreSR;
    float sweepOffset;
    float sweepDir = 1f;
    Vector3 fireVec;
    Vector3 sweepVec;
    Vector3 originWorldPos;
    bool isActive;

    void Awake()
    {
        switch (fireDirection)
        {
            case FireDirection.Right: fireVec = Vector3.right; sweepVec = Vector3.up;    break;
            case FireDirection.Left:  fireVec = Vector3.left;  sweepVec = Vector3.up;    break;
            case FireDirection.Down:  fireVec = Vector3.down;  sweepVec = Vector3.right; break;
            case FireDirection.Up:    fireVec = Vector3.up;    sweepVec = Vector3.right; break;
        }

        originWorldPos = transform.position;

        BuildEmitter();
        BuildLaserLines();
        BuildCollider();

        isActive = startsActive;
        if (!isActive) SetActive(false);
    }

    void BuildEmitter()
    {
        CreateRect("EmitterBody",
            Vector3.zero,
            new Vector3(emitterSize, emitterSize * 0.5f, 1f),
            new Color(0.15f, 0.08f, 0.08f, 1f), 5);

        emitterCoreSR = CreateRect("EmitterCore",
            Vector3.zero,
            new Vector3(emitterSize * 0.3f, emitterSize * 0.25f, 1f),
            emitterColor, 7);

        CreateRect("EmitterGlow",
            Vector3.zero,
            new Vector3(emitterSize * 2f, emitterSize * 0.8f, 1f),
            new Color(emitterColor.r, emitterColor.g, emitterColor.b, 0.08f), 4);
    }

    void BuildLaserLines()
    {
        outerLine = CreateLine("LaserOuter", outerWidth, outerColor, 1);
        innerLine = CreateLine("LaserInner", innerWidth, innerColor, 2);
    }

    void BuildCollider()
    {
        GameObject colGO = new GameObject("LaserCollider");
        colGO.transform.SetParent(transform);
        colGO.AddComponent<Hazard>();
        laserCollider           = colGO.AddComponent<BoxCollider2D>();
        laserCollider.isTrigger = true;
    }

    LineRenderer CreateLine(string name, float width, Color color, int sortOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        LineRenderer lr     = go.AddComponent<LineRenderer>();
        lr.useWorldSpace    = true;
        lr.positionCount    = 2;
        lr.startWidth       = width;
        lr.endWidth         = width;
        lr.material         = new Material(Shader.Find("Sprites/Default"));
        lr.startColor       = color;
        lr.endColor         = color;
        lr.sortingLayerName = "Default";
        lr.sortingOrder     = sortOrder;
        return lr;
    }

    SpriteRenderer CreateRect(string name, Vector3 localPos, Vector3 scale, Color color, int sortOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        SpriteRenderer sr   = go.AddComponent<SpriteRenderer>();
        sr.sprite           = sprite;
        sr.color            = color;
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = sortOrder;
        return sr;
    }

    public void SetActive(bool active)
    {
        isActive = active;

        innerLine.gameObject.SetActive(active);
        outerLine.gameObject.SetActive(active);
        laserCollider.gameObject.SetActive(active);

        if (emitterCoreSR != null)
            emitterCoreSR.color = active ? emitterColor : emitterDimColor;
    }

    public void TurnOn()  => SetActive(true);
    public void TurnOff() => SetActive(false);

    void Update()
    {
        if (!isActive) return;

        sweepOffset += sweepDir * sweepSpeed * Time.deltaTime;

        if (sweepOffset >  sweepDistance * 0.5f) { sweepOffset =  sweepDistance * 0.5f; sweepDir = -1f; }
        if (sweepOffset < -sweepDistance * 0.5f) { sweepOffset = -sweepDistance * 0.5f; sweepDir =  1f; }

        transform.position = originWorldPos + sweepVec * sweepOffset;

        UpdateLaser();
    }

    void UpdateLaser()
    {
        Vector3 origin = transform.position;

        // BoxCast thickness matches outerWidth so the cast can't clip past
        // a blocker's edge before also clearing anything at the same height.
        // A 1x1 blocker (frozen echo) and a 1x1 player at the same Y:
        // the cast clears the blocker top at the same moment it clears the player top.
        bool isHorizontal = fireDirection == FireDirection.Left
                         || fireDirection == FireDirection.Right;

        Vector2 castSize = isHorizontal
            ? new Vector2(outerWidth, outerWidth)
            : new Vector2(outerWidth, outerWidth);

        RaycastHit2D hit = Physics2D.BoxCast(
            origin, castSize, 0f,
            fireVec, maxLaserLength, solidLayers);

        Vector3 endpoint = hit.collider != null
            ? (Vector3)hit.point
            : origin + fireVec * maxLaserLength;

        float laserLength = Vector3.Distance(origin, endpoint);

        innerLine.SetPosition(0, origin);
        innerLine.SetPosition(1, endpoint);
        outerLine.SetPosition(0, origin);
        outerLine.SetPosition(1, endpoint);

        Vector3 colliderCenter = origin + fireVec * (laserLength * 0.5f);
        laserCollider.transform.position = colliderCenter;

        laserCollider.size = isHorizontal
            ? new Vector2(laserLength, innerWidth * 2f)
            : new Vector2(innerWidth * 2f, laserLength);
    }
}