using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private InventorySlotView slotPrefab;
    [SerializeField] private RectTransform itemContainer;
    [SerializeField] private InventoryItemView itemViewPrefab;
    [SerializeField] private float cellSize = 64f;
    [SerializeField] private GameObject invenUI;
    [SerializeField] private Color[] combinationColors =
    {
        new Color(1f, 0.78f, 0.12f, 1f),
        new Color(0.2f, 0.9f, 1f, 1f),
        new Color(1f, 0.35f, 0.72f, 1f),
        new Color(0.45f, 1f, 0.35f, 1f)
    };
    [SerializeField] private Color discardColor = new Color(0.55f, 0.16f, 0.12f, 1f);

    private InventorySlotView[,] slots;
    private InventoryCombinationService combinationService;
    private InventoryItemView activeDragView;
    private ItemInstance activeDragItem;
    private ItemInstance selectedItem;
    private Vector2 dragOffset;
    private int dragOriginalX;
    private int dragOriginalY;
    private bool dragOriginalRotated;
    private Vector2 lastPointerPosition;
    private Camera lastEventCamera;
    private Button catalogButton;
    private GameObject catalogPanel;
    private Text catalogText;
    private RectTransform combinationOverlayContainer;
    private RectTransform disassemblyOverlayContainer;
    private RectTransform discardOverlayContainer;
    private RectTransform selectionTooltipRect;
    private TextMeshProUGUI selectionTooltipText;
    private RectTransform equipmentSlotRect;
    private Image equipmentSlotImage;
    private Image equipmentIconImage;
    private TextMeshProUGUI equipmentLabel;

    private readonly Color equipmentSlotColor = new Color(0.12f, 0.1f, 0.08f, 0.96f);
    private readonly Color equipmentSlotHoverColor = new Color(0.82f, 0.62f, 0.16f, 1f);
    private readonly Color equipmentSlotOccupiedColor = new Color(0.18f, 0.34f, 0.2f, 0.96f);

    public InventoryMoveMode MoveMode => InventoryControlSettings.MoveMode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged += RefreshItems;
        InventoryControlSettings.OnMoveModeChanged += HandleMoveModeChanged;
    }

    private void OnDisable()
    {
        CancelActiveMove();

        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= RefreshItems;
        InventoryControlSettings.OnMoveModeChanged -= HandleMoveModeChanged;
    }

    private void OnDestroy()
    {
        if (combinationService != null)
            combinationService.OnCandidatesChanged -= HandleCandidatesChanged;

        if (DiscoveryManager.Instance != null)
            DiscoveryManager.Instance.OnDiscoveryChanged -= UpdateCatalogText;

        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        combinationService = inventoryManager.GetComponent<InventoryCombinationService>();
        if (combinationService != null)
            combinationService.OnCandidatesChanged += HandleCandidatesChanged;

        CreateSlots();
        EnsureCombinationOverlay();
        EnsureDisassemblyOverlay();
        EnsureDiscardOverlay();
        EnsureSelectionTooltip();
        EnsureCatalogUI();
        EnsureEquipmentUI();
        RefreshItems();
        combinationService?.RefreshCandidates();

        DiscoveryManager.GetOrCreate().OnDiscoveryChanged += UpdateCatalogText;
        UpdateCatalogText();
    }

    private void Update()
    {
        if (invenUI.activeSelf
            && MoveMode == InventoryMoveMode.ClickToClick
            && activeDragItem != null
            && activeDragView != null)
        {
            Drag(activeDragView, Input.mousePosition, lastEventCamera);
        }

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            CancelActiveMove();
            invenUI.SetActive(false);
            ClearSelection();
            return;
        }

        if (!invenUI.activeSelf || !Input.GetKeyDown(KeyCode.R) || selectedItem == null) return;

        if (activeDragItem == selectedItem)
        {
            RectTransform activeRect = activeDragView != null
                ? activeDragView.GetComponent<RectTransform>()
                : null;
            Vector2 oldSize = activeRect != null ? activeRect.sizeDelta : Vector2.one;
            Vector2 normalizedGrab = new Vector2(
                oldSize.x > 0f ? Mathf.Clamp01(-dragOffset.x / oldSize.x) : 0.5f,
                oldSize.y > 0f ? Mathf.Clamp01(dragOffset.y / oldSize.y) : 0.5f);

            inventoryManager.RotateDetachedItem(selectedItem);
            activeDragView?.RefreshVisual();
            if (activeRect != null)
            {
                Vector2 newSize = activeRect.sizeDelta;
                dragOffset = new Vector2(
                    -normalizedGrab.x * newSize.x,
                    normalizedGrab.y * newSize.y);
            }
            Drag(activeDragView, lastPointerPosition, lastEventCamera);
            return;
        }

        inventoryManager.TryRotateItem(selectedItem);
    }

    public void OpenBag()
    {
        if (invenUI.activeSelf) CancelActiveMove();
        invenUI.SetActive(!invenUI.activeSelf);
        if (!invenUI.activeSelf) ClearSelection();
    }

    public void SelectItem(ItemInstance item)
    {
        selectedItem = item;
        UpdateSelectionVisuals();
        UpdateActionButtons();
    }

    public void HandleItemClick(
        InventoryItemView view,
        ItemInstance item,
        Vector2 screenPosition,
        Camera eventCamera)
    {
        if (item == null) return;

        if (MoveMode == InventoryMoveMode.Drag)
        {
            SelectItem(item);
            return;
        }

        if (activeDragItem != null)
        {
            bool clickedActiveItem = activeDragItem == item;
            CancelActiveMove();
            if (!clickedActiveItem) SelectItem(item);
            return;
        }

        BeginClickMove(view, item, screenPosition, eventCamera);
    }

    public void HandleSlotClick(int x, int y)
    {
        if (MoveMode != InventoryMoveMode.ClickToClick || activeDragItem == null) return;
        if (activeDragView == null) return;
        Vector2Int previewPosition = AnchoredToGrid(
            activeDragView.GetComponent<RectTransform>().anchoredPosition);
        CompleteActiveMove(previewPosition.x, previewPosition.y);
    }

    public void HandleSlotHover(int x, int y)
    {
        // Click-to-click placement is previewed from the item following the cursor.
    }

    public void HandleSlotExit(int x, int y)
    {
        // Keep the current preview while the pointer crosses slot boundaries.
    }

    public void BeginDrag(InventoryItemView view, ItemInstance item, Vector2 screenPosition, Camera eventCamera)
    {
        if (MoveMode != InventoryMoveMode.Drag || !BeginMove(view, item)) return;
        lastPointerPosition = screenPosition;
        lastEventCamera = eventCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(itemContainer, screenPosition, eventCamera, out Vector2 localPoint);
        Vector2 pointerAnchored = LocalToAnchored(localPoint);
        dragOffset = view.GetComponent<RectTransform>().anchoredPosition - pointerAnchored;

        UpdateSelectionVisuals();
        Drag(view, screenPosition, eventCamera);
    }

    public void Drag(InventoryItemView view, Vector2 screenPosition, Camera eventCamera)
    {
        if (view == null || view != activeDragView || activeDragItem == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(itemContainer, screenPosition, eventCamera, out Vector2 localPoint)) return;

        lastPointerPosition = screenPosition;
        lastEventCamera = eventCamera;

        RectTransform rect = view.GetComponent<RectTransform>();
        rect.anchoredPosition = LocalToAnchored(localPoint) + dragOffset;

        Vector2Int gridPosition = AnchoredToGrid(rect.anchoredPosition);
        bool valid = inventoryManager.CanPlace(activeDragItem, gridPosition.x, gridPosition.y);
        ShowPlacementPreview(activeDragItem, gridPosition, valid);
        UpdateEquipmentSlotHighlight(screenPosition, eventCamera);
        UpdateSelectionTooltip();
    }

    public void EndDrag(InventoryItemView view, Vector2 screenPosition, Camera eventCamera)
    {
        if (view == null || view != activeDragView || activeDragItem == null) return;

        Drag(view, screenPosition, eventCamera);
        if (IsPointerOverEquipmentSlot(screenPosition, eventCamera)
            && CompleteActiveEquip())
            return;

        Vector2Int gridPosition = AnchoredToGrid(view.GetComponent<RectTransform>().anchoredPosition);
        CompleteActiveMove(gridPosition.x, gridPosition.y);
    }

    public int GetUnknownRecipeCount(ItemData item)
    {
        return combinationService != null ? combinationService.GetUnknownRecipeCount(item) : 0;
    }

    private void RefreshItems()
    {
        if (activeDragItem != null) return;

        foreach (Transform child in itemContainer)
        {
            if (child.GetComponent<InventoryItemView>() != null)
                Destroy(child.gameObject);
        }

        foreach (ItemInstance item in inventoryManager.items)
        {
            InventoryItemView itemView = Instantiate(itemViewPrefab, itemContainer);
            itemView.Init(item, cellSize, this);
            itemView.SetSelected(item == selectedItem);
        }

        if (selectedItem != null && !inventoryManager.items.Contains(selectedItem))
            selectedItem = null;

        UpdateEquipmentVisual();

        if (combinationOverlayContainer != null)
            combinationOverlayContainer.SetAsLastSibling();
        if (disassemblyOverlayContainer != null)
            disassemblyOverlayContainer.SetAsLastSibling();
        if (discardOverlayContainer != null)
            discardOverlayContainer.SetAsLastSibling();
        if (selectionTooltipRect != null)
            selectionTooltipRect.SetAsLastSibling();

        UpdateActionButtons();
    }

    private void CreateSlots()
    {
        int width = inventoryManager.gridWidth;
        int height = inventoryManager.gridHeight;
        float gridPixelWidth = width * cellSize;
        float gridPixelHeight = height * cellSize;

        SetupContainer(slotContainer, gridPixelWidth, gridPixelHeight);
        SetupContainer(itemContainer, gridPixelWidth, gridPixelHeight);
        slots = new InventorySlotView[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                InventorySlotView slot = Instantiate(slotPrefab, slotContainer);
                slot.Init(x, y, this);

                RectTransform rect = slot.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(cellSize, cellSize);
                rect.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
                slots[x, y] = slot;
            }
        }
    }

    private void SetupContainer(RectTransform container, float width, float height)
    {
        container.anchorMin = new Vector2(0.5f, 0.5f);
        container.anchorMax = new Vector2(0.5f, 0.5f);
        container.pivot = new Vector2(0.5f, 0.5f);
        container.anchoredPosition = Vector2.zero;
        container.sizeDelta = new Vector2(width, height);
    }

    private Vector2 LocalToAnchored(Vector2 localPoint)
    {
        return new Vector2(
            localPoint.x + itemContainer.rect.width * 0.5f,
            localPoint.y - itemContainer.rect.height * 0.5f);
    }

    private Vector2Int AnchoredToGrid(Vector2 anchoredPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt(anchoredPosition.x / cellSize),
            Mathf.RoundToInt(-anchoredPosition.y / cellSize));
    }

    private void ShowPlacementPreview(ItemInstance item, Vector2Int position, bool valid)
    {
        ClearPlacementPreview();
        InventoryPlacementState state = valid ? InventoryPlacementState.Valid : InventoryPlacementState.Invalid;

        for (int ix = 0; ix < item.Width; ix++)
        {
            for (int iy = 0; iy < item.Height; iy++)
            {
                int x = position.x + ix;
                int y = position.y + iy;
                if (x < 0 || y < 0 || x >= inventoryManager.gridWidth || y >= inventoryManager.gridHeight) continue;
                slots[x, y].SetHighlight(state);
            }
        }
    }

    private void ClearPlacementPreview()
    {
        if (slots == null) return;
        foreach (InventorySlotView slot in slots)
            slot?.SetHighlight(InventoryPlacementState.None);
    }

    private void ClearSelection()
    {
        selectedItem = null;
        UpdateSelectionVisuals();
        UpdateActionButtons();
    }

    private bool BeginMove(InventoryItemView view, ItemInstance item)
    {
        if (activeDragItem != null || item == null || view == null) return false;

        dragOriginalX = item.x;
        dragOriginalY = item.y;
        dragOriginalRotated = item.rotated;
        if (!inventoryManager.BeginItemDrag(item)) return false;

        ClearCombinationOverlays();
        ClearDisassemblyOverlay();
        ClearDiscardOverlay();
        activeDragView = view;
        activeDragItem = item;
        selectedItem = item;
        UpdateSelectionVisuals();
        UpdateSelectionTooltip();
        return true;
    }

    private void BeginClickMove(
        InventoryItemView view,
        ItemInstance item,
        Vector2 screenPosition,
        Camera eventCamera)
    {
        if (!BeginMove(view, item)) return;

        lastPointerPosition = screenPosition;
        lastEventCamera = eventCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            itemContainer,
            screenPosition,
            eventCamera,
            out Vector2 localPoint);
        Vector2 pointerAnchored = LocalToAnchored(localPoint);
        dragOffset = view.GetComponent<RectTransform>().anchoredPosition - pointerAnchored;
        view.SetRaycastBlocking(false);
        view.transform.SetAsLastSibling();
        Drag(view, screenPosition, eventCamera);
    }

    private void CompleteActiveMove(int targetX, int targetY)
    {
        if (activeDragItem == null) return;

        ItemInstance item = activeDragItem;
        int originalX = dragOriginalX;
        int originalY = dragOriginalY;
        bool originalRotated = dragOriginalRotated;
        activeDragView?.SetRaycastBlocking(true);
        activeDragView = null;
        activeDragItem = null;
        ClearPlacementPreview();
        ResetEquipmentSlotColor();
        inventoryManager.CompleteItemDrag(
            item,
            targetX,
            targetY,
            originalX,
            originalY,
            originalRotated);
    }

    private void CancelActiveMove()
    {
        if (activeDragItem == null) return;

        ItemInstance item = activeDragItem;
        activeDragView?.SetRaycastBlocking(true);
        activeDragView = null;
        activeDragItem = null;
        ClearPlacementPreview();
        ResetEquipmentSlotColor();
        inventoryManager.CompleteItemDrag(
            item,
            dragOriginalX,
            dragOriginalY,
            dragOriginalX,
            dragOriginalY,
            dragOriginalRotated);
    }

    private void HandleMoveModeChanged(InventoryMoveMode mode)
    {
        CancelActiveMove();
        ClearPlacementPreview();
        ClearSelection();
    }

    private bool CompleteActiveEquip()
    {
        if (activeDragItem == null || inventoryManager.EquippedItem != null)
            return false;

        ItemInstance item = activeDragItem;
        activeDragView?.SetRaycastBlocking(true);
        activeDragView = null;
        activeDragItem = null;
        selectedItem = null;
        ClearPlacementPreview();
        ResetEquipmentSlotColor();

        if (inventoryManager.EquipDetachedItem(item))
        {
            UpdateSelectionVisuals();
            UpdateActionButtons();
            return true;
        }

        inventoryManager.CompleteItemDrag(
            item,
            dragOriginalX,
            dragOriginalY,
            dragOriginalX,
            dragOriginalY,
            dragOriginalRotated);
        return false;
    }

    private void UpdateSelectionVisuals()
    {
        foreach (Transform child in itemContainer)
        {
            InventoryItemView view = child.GetComponent<InventoryItemView>();
            if (view != null) view.SetSelected(view.Item == selectedItem);
        }
    }

    private void HandleCandidatesChanged(IReadOnlyList<InventoryCombinationCandidate> candidates)
    {
        RenderCombinationCandidates(candidates);
    }

    private void EnsureCombinationOverlay()
    {
        if (combinationOverlayContainer != null) return;

        GameObject overlayObject = new GameObject("CombinationOverlays", typeof(RectTransform));
        overlayObject.transform.SetParent(itemContainer, false);
        combinationOverlayContainer = overlayObject.GetComponent<RectTransform>();
        combinationOverlayContainer.anchorMin = Vector2.zero;
        combinationOverlayContainer.anchorMax = Vector2.one;
        combinationOverlayContainer.offsetMin = Vector2.zero;
        combinationOverlayContainer.offsetMax = Vector2.zero;
        combinationOverlayContainer.SetAsLastSibling();
    }

    private void EnsureDisassemblyOverlay()
    {
        if (disassemblyOverlayContainer != null) return;

        GameObject overlayObject = new GameObject("DisassemblyOverlay", typeof(RectTransform));
        overlayObject.transform.SetParent(itemContainer, false);
        disassemblyOverlayContainer = overlayObject.GetComponent<RectTransform>();
        disassemblyOverlayContainer.anchorMin = Vector2.zero;
        disassemblyOverlayContainer.anchorMax = Vector2.one;
        disassemblyOverlayContainer.offsetMin = Vector2.zero;
        disassemblyOverlayContainer.offsetMax = Vector2.zero;
        disassemblyOverlayContainer.SetAsLastSibling();
    }

    private void EnsureDiscardOverlay()
    {
        if (discardOverlayContainer != null) return;

        GameObject overlayObject = new GameObject("DiscardOverlay", typeof(RectTransform));
        overlayObject.transform.SetParent(itemContainer, false);
        discardOverlayContainer = overlayObject.GetComponent<RectTransform>();
        discardOverlayContainer.anchorMin = Vector2.zero;
        discardOverlayContainer.anchorMax = Vector2.one;
        discardOverlayContainer.offsetMin = Vector2.zero;
        discardOverlayContainer.offsetMax = Vector2.zero;
        discardOverlayContainer.SetAsLastSibling();
    }

    private void EnsureSelectionTooltip()
    {
        if (selectionTooltipRect != null) return;

        GameObject tooltipObject = new GameObject(
            "SelectionTooltip",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline));
        tooltipObject.transform.SetParent(itemContainer, false);
        selectionTooltipRect = tooltipObject.GetComponent<RectTransform>();
        selectionTooltipRect.anchorMin = new Vector2(0f, 1f);
        selectionTooltipRect.anchorMax = new Vector2(0f, 1f);
        selectionTooltipRect.pivot = new Vector2(0.5f, 1f);

        Image background = tooltipObject.GetComponent<Image>();
        background.color = new Color(0.075f, 0.065f, 0.055f, 0.97f);
        background.raycastTarget = false;

        Outline outline = tooltipObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.63f, 0.32f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(selectionTooltipRect, false);
        selectionTooltipText = textObject.GetComponent<TextMeshProUGUI>();
        selectionTooltipText.alignment = TextAlignmentOptions.TopLeft;
        selectionTooltipText.color = Color.white;
        selectionTooltipText.fontSize = 17f;
        selectionTooltipText.enableAutoSizing = true;
        selectionTooltipText.fontSizeMin = 12f;
        selectionTooltipText.fontSizeMax = 17f;
        selectionTooltipText.raycastTarget = false;

        RectTransform textRect = selectionTooltipText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 8f);
        textRect.offsetMax = new Vector2(-10f, -8f);

        selectionTooltipRect.gameObject.SetActive(false);
    }

    private void UpdateSelectionTooltip()
    {
        EnsureSelectionTooltip();
        if (selectedItem == null
            || selectedItem.data == null
            || !inventoryManager.items.Contains(selectedItem))
        {
            selectionTooltipRect.gameObject.SetActive(false);
            return;
        }

        InventoryItemView selectedView = FindItemView(selectedItem);
        if (selectedView != null && selectedView.DisplayFont != null)
            selectionTooltipText.font = selectedView.DisplayFont;

        string itemName = string.IsNullOrWhiteSpace(selectedItem.data.itemName)
            ? selectedItem.data.itemId
            : selectedItem.data.itemName;
        string tooltipContent = $"<b>{itemName}</b>";

        ItemRecipe disassemblyRecipe = GetDisassemblyRecipe(selectedItem);
        if (disassemblyRecipe != null)
        {
            List<string> ingredientNames = new List<string>();
            foreach (RecipeIngredient ingredient in disassemblyRecipe.ingredients)
            {
                if (ingredient?.item == null) continue;
                string ingredientName = string.IsNullOrWhiteSpace(ingredient.item.itemName)
                    ? ingredient.item.itemId
                    : ingredient.item.itemName;
                ingredientNames.Add(ingredientName);
            }

            if (ingredientNames.Count > 0)
                tooltipContent += $"\n<color=#D8C8A8>해체 시: {string.Join(" + ", ingredientNames)}</color>";
        }
        else
        {
            tooltipContent += "\n<color=#A99F90>해체 불가</color>";
        }

        selectionTooltipText.text = tooltipContent;

        float containerWidth = Mathf.Max(120f, itemContainer.rect.width);
        float tooltipWidth = Mathf.Min(
            Mathf.Max(220f, selectedItem.Width * cellSize),
            containerWidth);
        Vector2 preferredSize = selectionTooltipText.GetPreferredValues(
            tooltipContent,
            tooltipWidth - 20f,
            0f);
        float tooltipHeight = Mathf.Max(54f, preferredSize.y + 16f);

        RectTransform selectedRect = activeDragItem == selectedItem && activeDragView != null
            ? activeDragView.GetComponent<RectTransform>()
            : null;
        float itemCenterX = selectedRect != null
            ? selectedRect.anchoredPosition.x + selectedRect.rect.width * 0.5f
            : (selectedItem.x + selectedItem.Width * 0.5f) * cellSize;
        float tooltipCenterX = Mathf.Clamp(
            itemCenterX,
            tooltipWidth * 0.5f,
            containerWidth - tooltipWidth * 0.5f);
        float itemBottomY = selectedRect != null
            ? selectedRect.anchoredPosition.y - selectedRect.rect.height
            : -(selectedItem.y + selectedItem.Height) * cellSize;

        selectionTooltipRect.sizeDelta = new Vector2(tooltipWidth, tooltipHeight);
        selectionTooltipRect.anchoredPosition = new Vector2(tooltipCenterX, itemBottomY - 8f);
        selectionTooltipRect.gameObject.SetActive(true);
        selectionTooltipRect.SetAsLastSibling();
    }

    private InventoryItemView FindItemView(ItemInstance item)
    {
        foreach (Transform child in itemContainer)
        {
            InventoryItemView view = child.GetComponent<InventoryItemView>();
            if (view != null && view.Item == item)
                return view;
        }

        return null;
    }

    private ItemRecipe GetDisassemblyRecipe(ItemInstance item)
    {
        if (item == null
            || combinationService == null
            || !combinationService.CanDisassemble(item))
            return null;

        return combinationService.RecipeDatabase?.GetById(item.createdByRecipeId);
    }

    private void RenderCombinationCandidates(IReadOnlyList<InventoryCombinationCandidate> candidates)
    {
        EnsureCombinationOverlay();
        ClearCombinationOverlays();
        if (candidates == null) return;

        for (int i = 0; i < candidates.Count; i++)
        {
            InventoryCombinationCandidate candidate = candidates[i];
            if (candidate == null || candidate.Recipe == null || candidate.MatchedItems == null || candidate.MatchedItems.Count == 0)
                continue;

            CreateCombinationOverlay(candidate, GetCombinationColor(i), i);
        }

        combinationOverlayContainer.SetAsLastSibling();
        if (discardOverlayContainer != null)
            discardOverlayContainer.SetAsLastSibling();
    }

    private Color GetCombinationColor(int candidateIndex)
    {
        if (combinationColors == null || combinationColors.Length == 0)
            return new Color(1f, 0.78f, 0.12f, 1f);

        return combinationColors[candidateIndex % combinationColors.Length];
    }

    private void ClearCombinationOverlays()
    {
        if (combinationOverlayContainer == null) return;
        foreach (Transform child in combinationOverlayContainer)
            Destroy(child.gameObject);
    }

    private void CreateCombinationOverlay(InventoryCombinationCandidate candidate, Color color, int index)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (ItemInstance matchedItem in candidate.MatchedItems)
        {
            if (matchedItem == null) continue;
            minX = Mathf.Min(minX, matchedItem.x);
            minY = Mathf.Min(minY, matchedItem.y);
            maxX = Mathf.Max(maxX, matchedItem.x + matchedItem.Width);
            maxY = Mathf.Max(maxY, matchedItem.y + matchedItem.Height);
        }

        if (minX == int.MaxValue) return;

        string recipeName = string.IsNullOrWhiteSpace(candidate.Recipe.recipeName)
            ? candidate.Recipe.recipeId
            : candidate.Recipe.recipeName;

        GameObject overlayObject = new GameObject($"CombinationCandidate {index}: {recipeName}", typeof(RectTransform));
        overlayObject.transform.SetParent(combinationOverlayContainer, false);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0f, 1f);
        overlayRect.anchorMax = new Vector2(0f, 1f);
        overlayRect.pivot = new Vector2(0f, 1f);
        overlayRect.anchoredPosition = new Vector2(minX * cellSize, -minY * cellSize);
        overlayRect.sizeDelta = new Vector2((maxX - minX) * cellSize, (maxY - minY) * cellSize);

        const float borderThickness = 4f;
        CreateBorder(overlayRect, "Top", color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, borderThickness));
        CreateBorder(overlayRect, "Bottom", color, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, borderThickness));
        CreateBorder(overlayRect, "Left", color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(borderThickness, 0f));
        CreateBorder(overlayRect, "Right", color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(borderThickness, 0f));

        Button button = CreateAttachedActionButton(overlayRect, color, "Combine", "합성");
        button.onClick.AddListener(() => combinationService.TryCombine(candidate));
    }

    private static void CreateBorder(
        RectTransform parent,
        string borderName,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta)
    {
        GameObject borderObject = new GameObject(borderName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        borderObject.transform.SetParent(parent, false);
        RectTransform rect = borderObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;

        Image image = borderObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static Button CreateAttachedActionButton(
        RectTransform parent,
        Color color,
        string objectName,
        string label,
        bool attachToLeft = false)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Vector2 topCorner = attachToLeft ? new Vector2(0f, 1f) : Vector2.one;
        rect.anchorMin = topCorner;
        rect.anchorMax = topCorner;
        rect.pivot = attachToLeft ? Vector2.one : new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(attachToLeft ? -6f : 6f, 0f);
        rect.sizeDelta = new Vector2(76f, 36f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonObject.GetComponent<Button>();
    }

    private void UpdateDisassemblyOverlay()
    {
        EnsureDisassemblyOverlay();
        ClearDisassemblyOverlay();
        if (combinationService == null || !combinationService.CanDisassemble(selectedItem)) return;

        ItemInstance itemToDisassemble = selectedItem;
        Color color = new Color(1f, 0.32f, 0.2f, 1f);
        GameObject overlayObject = new GameObject("DisassemblyCandidate", typeof(RectTransform));
        overlayObject.transform.SetParent(disassemblyOverlayContainer, false);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0f, 1f);
        overlayRect.anchorMax = new Vector2(0f, 1f);
        overlayRect.pivot = new Vector2(0f, 1f);
        overlayRect.anchoredPosition = new Vector2(itemToDisassemble.x * cellSize, -itemToDisassemble.y * cellSize);
        overlayRect.sizeDelta = new Vector2(itemToDisassemble.Width * cellSize, itemToDisassemble.Height * cellSize);

        const float borderThickness = 4f;
        CreateBorder(overlayRect, "Top", color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, borderThickness));
        CreateBorder(overlayRect, "Bottom", color, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, borderThickness));
        CreateBorder(overlayRect, "Left", color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(borderThickness, 0f));
        CreateBorder(overlayRect, "Right", color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(borderThickness, 0f));

        Button button = CreateAttachedActionButton(overlayRect, color, "Disassemble", "해체");
        button.onClick.AddListener(() =>
        {
            if (combinationService.TryDisassemble(itemToDisassemble))
                ClearSelection();
        });

        disassemblyOverlayContainer.SetAsLastSibling();
    }

    private void ClearDisassemblyOverlay()
    {
        if (disassemblyOverlayContainer == null) return;
        foreach (Transform child in disassemblyOverlayContainer)
            Destroy(child.gameObject);
    }

    private void UpdateDiscardOverlay()
    {
        EnsureDiscardOverlay();
        ClearDiscardOverlay();
        if (selectedItem == null
            || activeDragItem != null
            || !inventoryManager.items.Contains(selectedItem))
            return;

        ItemInstance itemToDiscard = selectedItem;
        GameObject overlayObject = new GameObject("DiscardCandidate", typeof(RectTransform));
        overlayObject.transform.SetParent(discardOverlayContainer, false);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0f, 1f);
        overlayRect.anchorMax = new Vector2(0f, 1f);
        overlayRect.pivot = new Vector2(0f, 1f);
        overlayRect.anchoredPosition = new Vector2(
            itemToDiscard.x * cellSize,
            -itemToDiscard.y * cellSize);
        overlayRect.sizeDelta = new Vector2(
            itemToDiscard.Width * cellSize,
            itemToDiscard.Height * cellSize);

        Button button = CreateAttachedActionButton(
            overlayRect,
            discardColor,
            "Discard",
            "\uBC84\uB9AC\uAE30",
            true);
        button.onClick.AddListener(() =>
        {
            if (selectedItem != itemToDiscard || activeDragItem != null) return;
            if (inventoryManager.DiscardItem(itemToDiscard))
                ClearSelection();
        });

        discardOverlayContainer.SetAsLastSibling();
    }

    private void ClearDiscardOverlay()
    {
        if (discardOverlayContainer == null) return;
        foreach (Transform child in discardOverlayContainer)
            Destroy(child.gameObject);
    }

    private void EnsureCatalogUI()
    {
        RectTransform parent = invenUI.transform as RectTransform;
        catalogButton = CreateActionButton(parent, "CatalogButton", "도감", new Vector2(-parent.rect.width + 210f, -parent.rect.height + 105f));

        catalogPanel = new GameObject("CatalogPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        catalogPanel.transform.SetParent(parent, false);
        RectTransform panelRect = catalogPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(30f, 0f);
        panelRect.sizeDelta = new Vector2(430f, 560f);
        catalogPanel.GetComponent<Image>().color = new Color(0.08f, 0.065f, 0.05f, 0.96f);

        GameObject textObject = new GameObject("CatalogText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(catalogPanel.transform, false);
        catalogText = textObject.GetComponent<Text>();
        catalogText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        catalogText.fontSize = 22;
        catalogText.alignment = TextAnchor.UpperLeft;
        catalogText.color = Color.white;
        catalogText.horizontalOverflow = HorizontalWrapMode.Wrap;
        catalogText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform textRect = catalogText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 24f);
        textRect.offsetMax = new Vector2(-24f, -24f);

        catalogButton.onClick.AddListener(() => catalogPanel.SetActive(!catalogPanel.activeSelf));
        catalogPanel.SetActive(false);
    }

    private void EnsureEquipmentUI()
    {
        if (equipmentSlotRect != null) return;

        RectTransform parent = invenUI.transform as RectTransform;
        GameObject slotObject = new GameObject(
            "EquipmentSlot",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        slotObject.transform.SetParent(parent, false);

        equipmentSlotRect = slotObject.GetComponent<RectTransform>();
        equipmentSlotRect.anchorMin = Vector2.one;
        equipmentSlotRect.anchorMax = Vector2.one;
        equipmentSlotRect.pivot = Vector2.one;
        equipmentSlotRect.anchoredPosition = new Vector2(-36f, -36f);
        equipmentSlotRect.sizeDelta = new Vector2(cellSize * 1.5f, cellSize * 1.5f);

        equipmentSlotImage = slotObject.GetComponent<Image>();
        equipmentSlotImage.color = equipmentSlotColor;

        GameObject iconObject = new GameObject(
            "Icon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        iconObject.transform.SetParent(slotObject.transform, false);
        equipmentIconImage = iconObject.GetComponent<Image>();
        equipmentIconImage.preserveAspect = true;
        equipmentIconImage.raycastTarget = false;
        RectTransform iconRect = equipmentIconImage.rectTransform;
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(10f, 28f);
        iconRect.offsetMax = new Vector2(-10f, -10f);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(slotObject.transform, false);
        equipmentLabel = labelObject.GetComponent<TextMeshProUGUI>();
        if (itemViewPrefab != null && itemViewPrefab.DisplayFont != null)
            equipmentLabel.font = itemViewPrefab.DisplayFont;
        equipmentLabel.text = "장착";
        equipmentLabel.alignment = TextAlignmentOptions.Center;
        equipmentLabel.color = Color.white;
        equipmentLabel.fontSize = 16f;
        equipmentLabel.enableAutoSizing = true;
        equipmentLabel.fontSizeMin = 10f;
        equipmentLabel.fontSizeMax = 16f;
        equipmentLabel.raycastTarget = false;
        RectTransform labelRect = equipmentLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0f, 26f);

        slotObject.GetComponent<Button>().onClick.AddListener(HandleEquipmentSlotClick);
        UpdateEquipmentVisual();
    }

    private void HandleEquipmentSlotClick()
    {
        if (activeDragItem != null)
        {
            CompleteActiveEquip();
            return;
        }

        if (inventoryManager.TryUnequipItem())
            ClearSelection();
    }

    private bool IsPointerOverEquipmentSlot(Vector2 screenPosition, Camera eventCamera)
    {
        return equipmentSlotRect != null
            && RectTransformUtility.RectangleContainsScreenPoint(
                equipmentSlotRect,
                screenPosition,
                eventCamera);
    }

    private void UpdateEquipmentSlotHighlight(Vector2 screenPosition, Camera eventCamera)
    {
        if (equipmentSlotImage == null) return;

        bool canEquip = inventoryManager.EquippedItem == null;
        equipmentSlotImage.color = canEquip
            && IsPointerOverEquipmentSlot(screenPosition, eventCamera)
                ? equipmentSlotHoverColor
                : GetEquipmentSlotColor();
    }

    private void UpdateEquipmentVisual()
    {
        if (equipmentIconImage == null || equipmentLabel == null) return;

        ItemInstance equippedItem = inventoryManager.EquippedItem;
        equipmentIconImage.sprite = equippedItem?.data != null
            ? equippedItem.data.icon
            : null;
        equipmentIconImage.enabled = equipmentIconImage.sprite != null;
        equipmentLabel.text = equippedItem?.data == null
            ? "장착"
            : (string.IsNullOrWhiteSpace(equippedItem.data.itemName)
                ? equippedItem.data.itemId
                : equippedItem.data.itemName);
        equipmentSlotRect.SetAsLastSibling();
        ResetEquipmentSlotColor();
    }

    private void ResetEquipmentSlotColor()
    {
        if (equipmentSlotImage != null)
            equipmentSlotImage.color = GetEquipmentSlotColor();
    }

    private Color GetEquipmentSlotColor()
    {
        return inventoryManager != null && inventoryManager.EquippedItem != null
            ? equipmentSlotOccupiedColor
            : equipmentSlotColor;
    }

    private void UpdateCatalogText()
    {
        if (catalogText == null) return;

        DiscoveryManager discovery = DiscoveryManager.GetOrCreate();
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine("발견한 아이템");
        if (discovery.DiscoveredItemIds.Count == 0) builder.AppendLine("- 아직 없음");
        foreach (string itemId in discovery.DiscoveredItemIds)
            builder.AppendLine($"- {itemId}");

        builder.AppendLine();
        builder.AppendLine("발견한 레시피");
        if (discovery.DiscoveredRecipeIds.Count == 0) builder.AppendLine("- 아직 없음");
        foreach (string recipeId in discovery.DiscoveredRecipeIds)
        {
            ItemRecipe recipe = combinationService?.RecipeDatabase?.GetById(recipeId);
            string displayName = recipe != null && !string.IsNullOrWhiteSpace(recipe.recipeName)
                ? recipe.recipeName
                : recipeId;
            builder.AppendLine($"- {displayName}");
        }

        catalogText.text = builder.ToString();
    }

    private static Button CreateActionButton(RectTransform parent, string objectName, string label, Vector2 position)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(180f, 48f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonObject.GetComponent<Button>();
    }

    private void UpdateActionButtons()
    {
        UpdateDisassemblyOverlay();
        UpdateDiscardOverlay();
        UpdateSelectionTooltip();
    }
}
