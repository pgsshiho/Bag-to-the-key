using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private RectTransform slotContainer;
    [SerializeField] private InventorySlotView slotPrefab;

    [SerializeField] private float cellSize = 64f;

    private InventorySlotView[,] slots;

    private void Start()
    {

        CreateSlots();
    }

    private void CreateSlots()
    {
        int width = inventoryManager.gridWidth;
        int height = inventoryManager.gridHeight;

        float gridPixelWidth = width * cellSize;
        float gridPixelHeight = height * cellSize;

        slotContainer.anchorMin = new Vector2(0.5f, 0.5f);
        slotContainer.anchorMax = new Vector2(0.5f, 0.5f);
        slotContainer.pivot = new Vector2(0.5f, 0.5f);
        slotContainer.anchoredPosition = Vector2.zero;
        slotContainer.sizeDelta = new Vector2(gridPixelWidth, gridPixelHeight);

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
                rect.anchoredPosition = new Vector2(
                    x * cellSize,
                    -y * cellSize
                );

                slots[x, y] = slot;
            }
        }
    }
}