using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Bright : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI buttonText;
    public Color Normal = Color.white;
    public Color Yellow = Color.yellow;

    void Awake()
    {
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = Yellow;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = Normal;
    }
    void OnEnable() => MainmenuManager.OnAnyButtonClicked += ResetColor;

    void OnDisable() => MainmenuManager.OnAnyButtonClicked -= ResetColor;
    private void ResetColor() => SetColor(Normal);
    private void SetColor(Color color)
    {
        if (buttonText != null) buttonText.color = color;
    }
}