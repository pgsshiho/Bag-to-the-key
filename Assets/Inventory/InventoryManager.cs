using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Size")]
    public int gridWidth = 10;
    public int gridHeight = 6;

    public InventoryGrid grid;

    [SerializeField] private ItemData testItem;

    private void Awake()
    {
        grid = new InventoryGrid(gridWidth, gridHeight);
    }

    private void Start()
    {
        ItemInstance item = new ItemInstance(testItem);

        if (grid.CanPlace(item, 0, 0))
        {
            grid.Place(item, 0, 0);
            Debug.Log("아이템 배치 성공");
        }
        else
        {
            Debug.Log("아이템 배치 실패");
        }
    }
}