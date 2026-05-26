using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    public LevelLoader levelLoader;

    [Header("Level Select Scene")]
    public string levelSelectScene = "LevelSelect";

    [Header("Colors")]
    public Color overlayColor  = new Color(0.04f, 0.04f, 0.1f,  0.88f);
    public Color textColor     = new Color(0.75f, 0.8f,  0.95f, 1f);
    public Color dimColor      = new Color(0.35f, 0.38f, 0.5f,  1f);
    public Color hoverColor    = new Color(0.6f,  0.7f,  1f,    1f);
    public Color exitHoverColor= new Color(1f,    0.75f, 0f,    1f);

    bool isPaused;
    GameObject overlay;

    void Start()
    {
        BuildOverlay();
        SetVisible(false);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        isPaused = !isPaused;
        SetVisible(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void Resume()
    {
        isPaused = false;
        SetVisible(false);
        Time.timeScale = 1f;
    }

    public void GoToLevelSelect()
    {
        Time.timeScale = 1f;
        if (levelLoader != null)
            levelLoader.LoadScene(levelSelectScene);
        else
            SceneManager.LoadScene(levelSelectScene);
    }

    void SetVisible(bool visible)
    {
        if (overlay != null)
            overlay.SetActive(visible);

        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.Confined : CursorLockMode.Confined;
    }
    void BuildOverlay()
    {
        // Canvas
        GameObject canvasGO = new GameObject("PauseCanvas");
        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 200;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        overlay = new GameObject("PauseOverlay", typeof(RectTransform));
        overlay.transform.SetParent(canvasGO.transform, false);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = overlayColor;
        StretchFull(overlay.GetComponent<RectTransform>());

        GameObject col = MakeElement("Col", overlay.transform);
        RectTransform colRT = col.GetComponent<RectTransform>();
        colRT.anchorMin = new Vector2(0.5f, 0.5f);
        colRT.anchorMax = new Vector2(0.5f, 0.5f);
        colRT.sizeDelta = new Vector2(320, 200);
        colRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 16;

        AddText(col.transform, "PAUSED", 11, dimColor, 30f, 14);

        AddMenuButton(col.transform, "RESUME", textColor, hoverColor, () => Resume());

        AddMenuButton(col.transform, "LEVEL SELECT", textColor, exitHoverColor, () => GoToLevelSelect());
    }

    void AddText(Transform parent, string text, float size, Color color, float height, int spacing = 0)
    {
        GameObject go = MakeElement("Txt", parent);
        go.AddComponent<LayoutElement>().preferredHeight = height;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = spacing;
    }

    void AddMenuButton(Transform parent, string label, Color normal, Color hover, UnityEngine.Events.UnityAction action)
    {
        GameObject go = MakeElement("Btn_" + label, parent);
        go.AddComponent<LayoutElement>().preferredHeight = 50;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0, 0, 0, 0);
        cb.highlightedColor = new Color(1, 1, 1, 0.04f);
        cb.pressedColor = new Color(1, 1, 1, 0.08f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);

        GameObject txtGO = MakeElement("Txt", go.transform);
        StretchFull(txtGO.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.color = normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        PauseButtonHover h = go.AddComponent<PauseButtonHover>();
        h.label = tmp;
        h.normalColor = normal;
        h.hoverColor = hover;
    }

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

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
