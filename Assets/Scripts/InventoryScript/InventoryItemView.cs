using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image), typeof(CanvasGroup))]
public class InventoryItemView
    : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerClickHandler
{
    private Image raycastImage;
    private Image iconImage;
    private Outline selectionOutline;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI unknownRecipeText;
    private RectTransform rectTransform;
    private InventoryUI inventoryUI;
    private ItemInstance item;
    private float cellSize;
    private bool isInitialized;

    public ItemInstance Item => item;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void Init(ItemInstance item, float cellSize, InventoryUI inventoryUI)
    {
        this.item = item;
        this.cellSize = cellSize;
        this.inventoryUI = inventoryUI;

        // The inventory panel is normally inactive while items are collected.
        // In that state Awake may not run before InventoryUI calls Init.
        if (!EnsureInitialized())
            return;

        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (!EnsureInitialized() || item == null || item.data == null)
            return;

        iconImage.sprite = item.data.icon;
        iconImage.preserveAspect = true;
        iconImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, item.rotated ? -90f : 0f);

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.sizeDelta = new Vector2(item.Width * cellSize, item.Height * cellSize);
        rectTransform.anchoredPosition = new Vector2(item.x * cellSize, -item.y * cellSize);

        int unknownCount = inventoryUI != null ? inventoryUI.GetUnknownRecipeCount(item.data) : 0;
        unknownRecipeText.text = unknownCount > 0 ? $"? {unknownCount}" : string.Empty;
    }

    public void SetSelected(bool selected)
    {
        if (selectionOutline != null)
            selectionOutline.enabled = selected;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventoryUI == null || item == null)
            return;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
        inventoryUI.BeginDrag(this, item, eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        inventoryUI?.Drag(this, eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        inventoryUI?.EndDrag(this, eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryUI == null || item == null)
            return;

        inventoryUI.SelectItem(item);
        if (eventData.button == PointerEventData.InputButton.Right)
            inventoryUI.ShowDisassembleAction(item);
    }

    private bool EnsureInitialized()
    {
        if (isInitialized)
            return true;

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError(
                $"{name}: InventoryItemView must be attached to a UI object with a RectTransform.",
                this
            );
            return false;
        }

        raycastImage = GetComponent<Image>();
        if (raycastImage == null)
            raycastImage = gameObject.AddComponent<Image>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        EnsureVisualChildren();
        isInitialized = iconImage != null && unknownRecipeText != null;
        return isInitialized;
    }

    private void EnsureVisualChildren()
    {
        raycastImage.sprite = null;
        raycastImage.color = new Color(1f, 1f, 1f, 0.001f);

        Transform existingIcon = transform.Find("Icon");
        if (existingIcon == null)
        {
            GameObject iconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline)
            );
            existingIcon = iconObject.transform;
            existingIcon.SetParent(transform, false);
        }

        iconImage = existingIcon.GetComponent<Image>();
        if (iconImage == null)
            iconImage = existingIcon.gameObject.AddComponent<Image>();
        iconImage.raycastTarget = false;
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(4f, 4f);
        iconRect.offsetMax = new Vector2(-4f, -4f);

        selectionOutline = existingIcon.GetComponent<Outline>();
        if (selectionOutline == null)
            selectionOutline = existingIcon.gameObject.AddComponent<Outline>();
        selectionOutline.effectDistance = new Vector2(3f, -3f);
        selectionOutline.enabled = false;

        Transform existingText = transform.Find("UnknownRecipes");
        if (existingText == null)
        {
            GameObject textObject = new GameObject(
                "UnknownRecipes",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );
            existingText = textObject.transform;
            existingText.SetParent(transform, false);
        }

        unknownRecipeText = existingText.GetComponent<TextMeshProUGUI>();
        if (unknownRecipeText == null)
            unknownRecipeText = existingText.gameObject.AddComponent<TextMeshProUGUI>();
        unknownRecipeText.raycastTarget = false;
        unknownRecipeText.alignment = TextAlignmentOptions.TopRight;
        unknownRecipeText.fontSize = 18f;
        RectTransform textRect = unknownRecipeText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 4f);
        textRect.offsetMax = new Vector2(-4f, -4f);
    }
}
