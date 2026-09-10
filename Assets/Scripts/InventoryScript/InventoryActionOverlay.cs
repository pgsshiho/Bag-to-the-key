using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryActionOverlay : MonoBehaviour
{
    [SerializeField] private Image[] borders;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text label;

    public void ResetForReuse()
    {
        actionButton.onClick.RemoveAllListeners();
    }

    public Button Configure(Color color, string caption, bool attachToLeft = false, bool showBorder = true)
    {
        foreach (Image border in borders)
        {
            border.color = color;
            border.gameObject.SetActive(showBorder);
        }

        actionButton.image.color = color;
        label.text = caption;
        RectTransform rect = (RectTransform)actionButton.transform;
        Vector2 corner = attachToLeft ? new Vector2(0f, 1f) : Vector2.one;
        rect.anchorMin = rect.anchorMax = corner;
        rect.pivot = attachToLeft ? Vector2.one : new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(attachToLeft ? -6f : 6f, 0f);
        return actionButton;
    }
}
