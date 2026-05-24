using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Level Select screen. Editorial typographic layout — no button borders,
// large floating numbers, hover accent. Two sections: Basic and Experimental.
// Entire UI built from code — no prefabs needed.
public class LevelSelectUI : MonoBehaviour
{
    [Header("References")]
    public SceneList sceneList;
    public LevelLoader levelLoader;

    [Header("Layout")]
    [Tooltip("First N levels are Basic. Rest are Experimental.")]
    public int basicLevelCount = 5;

    [Header("Colors")]
    public Color bgColor         = new Color(0.06f, 0.06f, 0.12f, 1f);
    public Color labelColor      = new Color(0.3f,  0.32f, 0.45f, 1f);
    public Color numberColor     = new Color(0.75f, 0.8f,  0.95f, 1f);
    public Color hoverBasic      = new Color(0.6f,  0.7f,  1f,    1f);
    public Color hoverExp        = new Color(1f,    0.75f, 0f,    1f);
    public Color dividerColor    = new Color(0.15f, 0.16f, 0.25f, 1f);

    Canvas canvas;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        BuildUI();
    }

    void BuildUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background
        GameObject bg = MakeElement("BG", canvasGO.transform);
        bg.AddComponent<Image>().color = bgColor;
        StretchFull(bg.GetComponent<RectTransform>());

        // Centered column
        GameObject col = MakeElement("Col", canvasGO.transform);
        RectTransform colRT = col.GetComponent<RectTransform>();
        colRT.anchorMin = new Vector2(0.5f, 0.5f);
        colRT.anchorMax = new Vector2(0.5f, 0.5f);
        colRT.sizeDelta = new Vector2(800, 600);
        colRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 0;
        vlg.padding = new RectOffset(0, 0, 60, 0);

        // "LEVEL SELECT" label
        AddLabel(col.transform, "LEVEL SELECT", 11, labelColor, 50f, 12);

        // Divider
        AddDivider(col.transform, 40f);

        // Basic section label
        AddLabel(col.transform, "BASIC", 9, labelColor, 48f, 10);

        // Basic level numbers row
        AddLevelRow(col.transform, 0, basicLevelCount, hoverBasic);

        // Gap between sections
        AddSpacer(col.transform, 40f);

        // Divider
        AddDivider(col.transform, 30f);

        // Experimental section
        int expCount = sceneList.levels.Length - basicLevelCount;
        if (expCount > 0)
        {
            AddLabel(col.transform, "EXPERIMENTAL", 9, labelColor, 48f, 10);
            AddLevelRow(col.transform, basicLevelCount, expCount, hoverExp);
        }
    }

    // ───────────────────────────────────────────
    // SECTION BUILDERS
    // ───────────────────────────────────────────

    void AddLabel(Transform parent, string text, float size, Color color, float height, int letterSpacing = 0)
    {
        GameObject go = MakeElement("Lbl", parent);
        go.AddComponent<LayoutElement>().preferredHeight = height;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = letterSpacing;
    }

    void AddDivider(Transform parent, float height)
    {
        GameObject go = MakeElement("Div", parent);
        go.AddComponent<LayoutElement>().preferredHeight = height;

        GameObject line = MakeElement("Line", go.transform);
        Image img = line.AddComponent<Image>();
        img.color = dividerColor;
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.5f);
        rt.anchorMax = new Vector2(0.9f, 0.5f);
        rt.sizeDelta = new Vector2(0, 1);
        rt.anchoredPosition = Vector2.zero;
    }

    void AddSpacer(Transform parent, float height)
    {
        GameObject go = MakeElement("Sp", parent);
        go.AddComponent<LayoutElement>().preferredHeight = height;
    }

    void AddLevelRow(Transform parent, int startIndex, int count, Color accentColor)
    {
        GameObject row = MakeElement("Row", parent);
        row.AddComponent<LayoutElement>().preferredHeight = 90;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 8;

        for (int i = 0; i < count; i++)
        {
            int sceneIndex = startIndex + i;
            if (sceneIndex >= sceneList.levels.Length) break;
            AddLevelNumber(row.transform, sceneIndex, i + 1, accentColor);
        }
    }

    void AddLevelNumber(Transform parent, int sceneIndex, int displayNum, Color accentColor)
    {
        string sceneName = sceneList.levels[sceneIndex];

        GameObject btn = MakeElement("L" + displayNum, parent);
        LayoutElement le = btn.AddComponent<LayoutElement>();
        le.preferredWidth  = 80;
        le.preferredHeight = 80;

        // Transparent background — no border, no box
        Image img = btn.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);

        Button b = btn.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.normalColor      = new Color(0, 0, 0, 0);
        cb.highlightedColor = new Color(1, 1, 1, 0.04f);
        cb.pressedColor     = new Color(1, 1, 1, 0.08f);
        cb.fadeDuration     = 0.08f;
        b.colors = cb;
        b.targetGraphic = img;

        string toLoad = sceneName;
        b.onClick.AddListener(() => LoadLevel(toLoad));

        // Large number text
        GameObject numGO = MakeElement("Num", btn.transform);
        StretchFull(numGO.GetComponent<RectTransform>());
        TextMeshProUGUI numTmp = numGO.AddComponent<TextMeshProUGUI>();
        numTmp.text = displayNum.ToString();
        numTmp.fontSize = 38;
        numTmp.color = numberColor;
        numTmp.alignment = TextAlignmentOptions.Center;
        numTmp.fontStyle = FontStyles.Bold;

        // Hover color swap via pointer events
        LevelButtonHover hover = btn.AddComponent<LevelButtonHover>();
        hover.label = numTmp;
        hover.normalColor = numberColor;
        hover.hoverColor = accentColor;
    }

    // ───────────────────────────────────────────
    // LOAD
    // ───────────────────────────────────────────

    void LoadLevel(string sceneName)
    {
        if (levelLoader != null)
            levelLoader.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    // ───────────────────────────────────────────
    // HELPERS
    // ───────────────────────────────────────────

    GameObject MakeElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}