using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// Swaps the number label color on hover.
// Attached by LevelSelectUI to each level button.
public class LevelButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI label;
    public Color normalColor;
    public Color hoverColor;

    public void OnPointerEnter(PointerEventData e) => label.color = hoverColor;
    public void OnPointerExit(PointerEventData e)  => label.color = normalColor;
}
