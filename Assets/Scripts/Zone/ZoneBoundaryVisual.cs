using UnityEngine;
using System.Collections;
using TMPro;

public class ZoneBoundaryVisual : MonoBehaviour
{
    [Header("Zone Color")]
    public Color zoneColor = new Color(0f, 0.9f, 1f, 1f);

    [Header("Boundary Lines")]
    public float lineWidth  = 0.08f;
    public float lineHeight = 50f;

    [Header("Zone Fill")]
    public float fillAlpha = 0.04f;

    [Header("Screen Flash")]
    public float flashAlpha    = 0.25f;
    public float flashDuration = 0.15f;

    [Header("Riddle")]
    public string riddleText            = "Speed is yours.\nDirection is theirs.";
    public float  riddleDisplayDuration = 3f;
    public float  riddleFadeDuration    = 0.5f;
    public float  riddleFollowOffset    = 2.5f; // units above player
    public float  riddleFontSize        = 0.5f;

    // Internal
    bool   riddleShown;
    bool   riddleRunning;
    Canvas flashCanvas;
    UnityEngine.UI.Image flashImage;
    TextMeshPro riddleTMP;
    Transform   playerTransform;
    Sprite      whiteSprite;

    void Awake()
    {
        whiteSprite = CreateWhiteSprite();

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        Vector2 size   = col.size;
        Vector2 offset = col.offset;

        float left  = offset.x - size.x * 0.5f;
        float right = offset.x + size.x * 0.5f;

        CreateBoundaryLine("BoundaryLeft",
            new Vector3(left, offset.y, 0f),
            new Vector3(lineWidth, lineHeight, 1f));

        CreateBoundaryLine("BoundaryRight",
            new Vector3(right, offset.y, 0f),
            new Vector3(lineWidth, lineHeight, 1f));

        GameObject fill = new GameObject("ZoneFill");
        fill.transform.SetParent(transform);
        fill.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        fill.transform.localScale    = new Vector3(size.x, lineHeight, 1f);
        SpriteRenderer fillSR   = fill.AddComponent<SpriteRenderer>();
        fillSR.sprite           = whiteSprite;
        fillSR.color            = new Color(zoneColor.r, zoneColor.g, zoneColor.b, fillAlpha);
        fillSR.sortingLayerName = "Default";
        fillSR.sortingOrder     = -5;

        BuildFlashCanvas();
        BuildRiddleText();
    }

    void Update()
    {
        // Keep text above player while riddle is running
        if (riddleRunning && riddleTMP != null && playerTransform != null)
        {
            riddleTMP.transform.position = new Vector3(
                playerTransform.position.x,
                playerTransform.position.y + riddleFollowOffset,
                0f);
        }
    }

    void CreateBoundaryLine(string name, Vector3 localPos, Vector3 scale)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(transform);
        line.transform.localPosition = localPos;
        line.transform.localScale    = scale;

        SpriteRenderer sr   = line.AddComponent<SpriteRenderer>();
        sr.sprite           = whiteSprite;
        sr.color            = zoneColor;
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = -4;
    }

    void BuildFlashCanvas()
    {
        GameObject canvasGO = new GameObject("ZoneFlashCanvas");
        canvasGO.transform.SetParent(transform, false);

        flashCanvas              = canvasGO.AddComponent<Canvas>();
        flashCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        flashCanvas.sortingOrder = 100;

        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject imgGO = new GameObject("FlashImage");
        imgGO.transform.SetParent(canvasGO.transform, false);

        flashImage               = imgGO.AddComponent<UnityEngine.UI.Image>();
        flashImage.color         = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0f);
        flashImage.raycastTarget = false;

        RectTransform rt    = imgGO.GetComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    void BuildRiddleText()
    {
        if (string.IsNullOrEmpty(riddleText)) return;

        GameObject textGO = new GameObject("RiddleText");
        textGO.transform.SetParent(transform);
        textGO.transform.position = new Vector3(0f, -1000f, 0f);

        riddleTMP                  = textGO.AddComponent<TextMeshPro>();
        riddleTMP.text             = riddleText;
        riddleTMP.fontSize         = riddleFontSize;
        riddleTMP.color            = new Color(1f, 1f, 1f, 0f);
        riddleTMP.alignment        = TextAlignmentOptions.Center;
        riddleTMP.textWrappingMode = TextWrappingModes.Normal;
        riddleTMP.sortingOrder     = 10;

        RectTransform rt = textGO.GetComponent<RectTransform>();
        rt.sizeDelta     = new Vector2(6f, 2f);
    }

    public void TriggerScreenFlash()
    {
        if (flashImage == null) return;
        StartCoroutine(FlashRoutine());
    }

    public void TriggerRiddle(Transform player)
    {
        if (riddleShown || riddleTMP == null) return;
        riddleShown     = true;
        playerTransform = player;
        StartCoroutine(RiddleRoutine());
    }

    IEnumerator FlashRoutine()
    {
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(flashAlpha, 0f, elapsed / flashDuration);
            flashImage.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, alpha);
            yield return null;
        }
        flashImage.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0f);
    }

    IEnumerator RiddleRoutine()
    {
        riddleRunning = true;

        // Fade in
        float elapsed = 0f;
        while (elapsed < riddleFadeDuration)
        {
            elapsed += Time.deltaTime;
            riddleTMP.color = new Color(1f, 1f, 1f,
                Mathf.Lerp(0f, 0.9f, elapsed / riddleFadeDuration));
            yield return null;
        }

        yield return new WaitForSeconds(riddleDisplayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < riddleFadeDuration)
        {
            elapsed += Time.deltaTime;
            riddleTMP.color = new Color(1f, 1f, 1f,
                Mathf.Lerp(0.9f, 0f, elapsed / riddleFadeDuration));
            yield return null;
        }

        riddleTMP.color = new Color(1f, 1f, 1f, 0f);
        riddleRunning   = false;
        playerTransform = null;

        // Park text offscreen
        riddleTMP.transform.position = new Vector3(0f, -1000f, 0f);
    }

    Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}