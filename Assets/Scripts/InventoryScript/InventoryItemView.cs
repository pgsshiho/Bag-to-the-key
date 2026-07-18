using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class InventoryItemView : MonoBehaviour
{
    private Image iconImage;

    private void Awake()
    {
        iconImage = GetComponent<Image>();
    }

    public void Init(ItemInstance item, float cellSize)
    {
        iconImage.sprite = item.data.icon;
        iconImage.preserveAspect = true;

        RectTransform rect = GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);

        rect.sizeDelta = new Vector2(
            item.Width * cellSize,
            item.Height * cellSize
        );

        rect.anchoredPosition = new Vector2(
            item.x * cellSize,
            -item.y * cellSize
        );
    }
}