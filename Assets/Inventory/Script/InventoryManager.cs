using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public event Action OnInventoryChanged;

    [Header("Inventory Size")]
    public int gridWidth = 10;
    public int gridHeight = 6;

    public InventoryGrid grid;
    public List<ItemInstance> items = new List<ItemInstance>();

    private void Awake()
    {
        grid = new InventoryGrid(gridWidth, gridHeight);
    }

    public bool AddItem(ItemData itemData)
    {
        ItemInstance newItem = new ItemInstance(itemData);

        if (grid.TryFindSpace(newItem, out Vector2Int position))
        {
            grid.Place(newItem, position.x, position.y);
            items.Add(newItem);

            OnInventoryChanged?.Invoke();

            Debug.Log($"{itemData.itemName} 획득 성공");
            return true;
        }

        Debug.Log($"{itemData.itemName} 넣을 공간 없음");
        return false;
    }
}