using UnityEngine;
using UnityEngine.Events;

// A lever switch that toggles between two states when the player
// touches it from either side. Flips visually and invokes events.
// Place a BoxCollider2D (IsTrigger) on this GameObject.
public class LevelSwitch : MonoBehaviour
{
    [Header("Visual")]
    public float leverWidth  = 0.12f;
    public float leverHeight = 0.6f;
    public float baseWidth   = 0.3f;
    public float baseHeight  = 0.1f;
    public Color leverColor  = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color baseColor   = new Color(0.3f, 0.3f, 0.3f, 1f);
    public float leanAngle   = 30f;

    [Header("State")]
    public bool startsOn = false;

    [Header("Events")]
    public UnityEvent OnActivate;
    public UnityEvent OnDeactivate;

    bool isOn;
    GameObject leverArm;
    string playerTag = "Player";

    void Awake()
    {
        isOn = startsOn;
        BuildVisual();
        UpdateLeverVisual();
    }

    void Start()
    {
        // Fire after all Awakes have run — prevents null refs on other scripts
        if (isOn)
            OnActivate?.Invoke();
        else
            OnDeactivate?.Invoke();
    }

    void BuildVisual()
    {
        // Base block
        GameObject basePart = new GameObject("SwitchBase");
        basePart.transform.SetParent(transform);
        basePart.transform.localPosition = new Vector3(0f, -leverHeight * 0.4f, 0f);
        basePart.transform.localScale    = new Vector3(baseWidth, baseHeight, 1f);

        SpriteRenderer baseSR   = basePart.AddComponent<SpriteRenderer>();
        baseSR.sprite           = GetWhiteSprite();
        baseSR.color            = baseColor;
        baseSR.sortingLayerName = "Default";
        baseSR.sortingOrder     = 5;

        // Lever arm
        leverArm = new GameObject("LeverArm");
        leverArm.transform.SetParent(transform);
        leverArm.transform.localPosition = Vector3.zero;
        leverArm.transform.localScale    = new Vector3(leverWidth, leverHeight, 1f);

        SpriteRenderer leverSR   = leverArm.AddComponent<SpriteRenderer>();
        leverSR.sprite           = GetWhiteSprite();
        leverSR.color            = leverColor;
        leverSR.sortingLayerName = "Default";
        leverSR.sortingOrder     = 6;

        // Knob at top of lever
        GameObject knob = new GameObject("LeverKnob");
        knob.transform.SetParent(leverArm.transform);
        knob.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        knob.transform.localScale    = new Vector3(2.5f, 0.4f, 1f);

        SpriteRenderer knobSR   = knob.AddComponent<SpriteRenderer>();
        knobSR.sprite           = GetWhiteSprite();
        knobSR.color            = leverColor;
        knobSR.sortingLayerName = "Default";
        knobSR.sortingOrder     = 7;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        Toggle();
    }

    void Toggle()
    {
        isOn = !isOn;
        UpdateLeverVisual();

        if (isOn)
            OnActivate?.Invoke();
        else
            OnDeactivate?.Invoke();
    }

    void UpdateLeverVisual()
    {
        float angle = isOn ? -leanAngle : leanAngle;
        leverArm.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    Sprite GetWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}