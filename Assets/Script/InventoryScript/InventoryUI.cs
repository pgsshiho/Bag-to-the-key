using UnityEngine;

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

    private InventorySlotView[,] slots;

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
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= RefreshItems;
    }

    private void Start()
    {
        CreateSlots();
        RefreshItems();
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            invenUI.SetActive(false);
        }
    }

    private void RefreshItems()
    {
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (ItemInstance item in inventoryManager.items)
        {
            InventoryItemView itemView = Instantiate(itemViewPrefab, itemContainer);
            itemView.Init(item, cellSize);
        }
    }
    public void OpenBag()
    {
        invenUI.SetActive(!invenUI.activeSelf);
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
}