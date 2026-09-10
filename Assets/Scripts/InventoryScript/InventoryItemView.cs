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
    [SerializeField] private Image raycastImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Outline selectionOutline;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI unknownRecipeText;
    private RectTransform rectTransform;
    private InventoryUI inventoryUI;
    private ItemInstance item;
    private float cellSize;
    private bool isInitialized;

    public ItemInstance Item => item;
    public TMP_FontAsset DisplayFont => unknownRecipeText != null ? unknownRecipeText.font : null;
    public RectTransform RectTransform
    {
        get
        {
            EnsureInitialized();
            return rectTransform;
        }
    }

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

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.sizeDelta = new Vector2(item.Width * cellSize, item.Height * cellSize);
        rectTransform.anchoredPosition = new Vector2(item.x * cellSize, -item.y * cellSize);

        iconImage.sprite = item.data.icon;
        iconImage.preserveAspect = true;
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(
            Mathf.Max(0f, item.data.width * cellSize - 8f),
            Mathf.Max(0f, item.data.height * cellSize - 8f));
        iconRect.localEulerAngles = new Vector3(0f, 0f, item.rotated ? -90f : 0f);

        int unknownCount = inventoryUI != null ? inventoryUI.GetUnknownRecipeCount(item.data) : 0;
        unknownRecipeText.text = unknownCount > 0 ? $"? {unknownCount}" : string.Empty;
    }

    public void SetSelected(bool selected)
    {
        if (selectionOutline != null)
            selectionOutline.enabled = selected;
    }

    public void SetRaycastBlocking(bool blocksRaycasts)
    {
        if (EnsureInitialized()) canvasGroup.blocksRaycasts = blocksRaycasts;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventoryUI == null || item == null || inventoryUI.MoveMode != InventoryMoveMode.Drag)
            return;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
        inventoryUI.BeginDrag(this, item, eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (inventoryUI != null && inventoryUI.MoveMode == InventoryMoveMode.Drag)
            inventoryUI.Drag(this, eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        if (inventoryUI != null && inventoryUI.MoveMode == InventoryMoveMode.Drag)
            inventoryUI.EndDrag(this, eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryUI == null || item == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
            inventoryUI.HandleItemClick(this, item, eventData.position, eventData.pressEventCamera);
    }

    private bool EnsureInitialized()
    {
        if (isInitialized)
            return true;

        if (!TryGetComponent(out rectTransform))
        {
            Debug.LogError(
                $"{name}: InventoryItemView must be attached to a UI object with a RectTransform.",
                this
            );
            return false;
        }

        isInitialized = raycastImage != null && canvasGroup != null
            && iconImage != null && selectionOutline != null && unknownRecipeText != null;
        if (!isInitialized)
            Debug.LogError($"{name}: InventoryItemView prefab is missing its authored UI references.", this);
        return isInitialized;
    }

}
