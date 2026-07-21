using System.Collections.Generic;
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
    }

    private void OnDisable()
    {
        CancelActiveDrag();

        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= RefreshItems;
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
        EnsureCatalogUI();
        RefreshItems();
        combinationService?.RefreshCandidates();

        DiscoveryManager.GetOrCreate().OnDiscoveryChanged += UpdateCatalogText;
        UpdateCatalogText();
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            CancelActiveDrag();
            invenUI.SetActive(false);
            ClearSelection();
            return;
        }

        if (!invenUI.activeSelf || !Input.GetKeyDown(KeyCode.R) || selectedItem == null) return;

        if (activeDragItem == selectedItem)
        {
            inventoryManager.RotateDetachedItem(selectedItem);
            activeDragView.RefreshVisual();
            Drag(activeDragView, lastPointerPosition, lastEventCamera);
            return;
        }

        inventoryManager.TryRotateItem(selectedItem);
    }

    public void OpenBag()
    {
        if (invenUI.activeSelf) CancelActiveDrag();
        invenUI.SetActive(!invenUI.activeSelf);
        if (!invenUI.activeSelf) ClearSelection();
    }

    public void SelectItem(ItemInstance item)
    {
        selectedItem = item;
        UpdateSelectionVisuals();
        UpdateDisassemblyOverlay();
    }

    public void BeginDrag(InventoryItemView view, ItemInstance item, Vector2 screenPosition, Camera eventCamera)
    {
        if (activeDragItem != null || item == null) return;
        if (!inventoryManager.BeginItemDrag(item)) return;

        ClearCombinationOverlays();
        ClearDisassemblyOverlay();

        activeDragView = view;
        activeDragItem = item;
        selectedItem = item;
        dragOriginalX = item.x;
        dragOriginalY = item.y;
        dragOriginalRotated = item.rotated;
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
    }

    public void EndDrag(InventoryItemView view, Vector2 screenPosition, Camera eventCamera)
    {
        if (view == null || view != activeDragView || activeDragItem == null) return;

        Drag(view, screenPosition, eventCamera);
        Vector2Int gridPosition = AnchoredToGrid(view.GetComponent<RectTransform>().anchoredPosition);
        ItemInstance item = activeDragItem;
        int originalX = dragOriginalX;
        int originalY = dragOriginalY;
        bool originalRotated = dragOriginalRotated;

        activeDragView = null;
        activeDragItem = null;
        ClearPlacementPreview();
        inventoryManager.CompleteItemDrag(
            item,
            gridPosition.x,
            gridPosition.y,
            originalX,
            originalY,
            originalRotated);
    }

    public void ShowDisassembleAction(ItemInstance item)
    {
        SelectItem(item);
        if (combinationService != null && combinationService.TryDisassemble(item))
            ClearSelection();
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

        if (combinationOverlayContainer != null)
            combinationOverlayContainer.SetAsLastSibling();
        if (disassemblyOverlayContainer != null)
            disassemblyOverlayContainer.SetAsLastSibling();

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
                slot.Init(x, y);

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

    private void CancelActiveDrag()
    {
        if (activeDragItem == null) return;

        ItemInstance item = activeDragItem;
        activeDragView = null;
        activeDragItem = null;
        ClearPlacementPreview();
        inventoryManager.CompleteItemDrag(
            item,
            dragOriginalX,
            dragOriginalY,
            dragOriginalX,
            dragOriginalY,
            dragOriginalRotated);
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
        string label)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(6f, 0f);
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
    }
}
