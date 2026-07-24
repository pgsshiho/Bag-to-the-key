using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Bright : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI buttonText;
    private Tween colorTween;

    public Color Normal = Color.white;
    public Color Yellow = Color.yellow;
    [SerializeField, Min(0f)] private float colorTransitionDuration = 0.12f;

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

    void OnDisable()
    {
        MainmenuManager.OnAnyButtonClicked -= ResetColor;
        colorTween?.Kill();
        colorTween = null;
        if (buttonText != null) buttonText.color = Normal;
    }

    private void ResetColor() => SetColor(Normal);

    private void SetColor(Color color)
    {
        if (buttonText == null) return;

        colorTween?.Kill();
        if (colorTransitionDuration <= 0f)
        {
            buttonText.color = color;
            colorTween = null;
            return;
        }

        colorTween = DOTween
            .To(
                () => buttonText.color,
                value => buttonText.color = value,
                color,
                colorTransitionDuration)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => colorTween = null);
    }
}
