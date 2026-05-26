using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PauseButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI label;
    public Color normalColor;
    public Color hoverColor;

    public void OnPointerEnter(PointerEventData e) => label.color = hoverColor;
    public void OnPointerExit(PointerEventData e)  => label.color = normalColor;
}
