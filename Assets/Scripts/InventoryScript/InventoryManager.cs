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

        if (GetComponent<InventoryCombinationService>() == null)
            gameObject.AddComponent<InventoryCombinationService>();
    }

    public bool AddItem(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning("ItemData가 없어 아이템을 획득할 수 없습니다.");
            return false;
        }

        ItemInstance newItem = new ItemInstance(itemData);
        if (!grid.TryFindSpace(newItem, out Vector2Int position))
        {
            Debug.Log($"{itemData.itemName}을(를) 넣을 공간이 없습니다.");
            return false;
        }

        if (!grid.Place(newItem, position.x, position.y)) return false;

        items.Add(newItem);
        OnInventoryChanged?.Invoke();
        Debug.Log($"{itemData.itemName} 획득");
        return true;
    }

    public bool TryMoveItem(ItemInstance item, int targetX, int targetY)
    {
        if (!items.Contains(item)) return false;

        bool moved = grid.TryMove(item, targetX, targetY);
        OnInventoryChanged?.Invoke();
        return moved;
    }

    public bool BeginItemDrag(ItemInstance item)
    {
        if (!items.Contains(item)) return false;
        grid.Remove(item);
        return true;
    }

    public bool CompleteItemDrag(
        ItemInstance item,
        int targetX,
        int targetY,
        int originalX,
        int originalY,
        bool originalRotated)
    {
        if (!items.Contains(item)) return false;

        bool placed = grid.Place(item, targetX, targetY);
        if (!placed)
        {
            item.rotated = originalRotated;
            grid.Place(item, originalX, originalY);
        }

        OnInventoryChanged?.Invoke();
        return placed;
    }

    public void RotateDetachedItem(ItemInstance item)
    {
        if (items.Contains(item)) item.Rotate();
    }

    public bool TryRotateItem(ItemInstance item, bool notify = true)
    {
        if (!items.Contains(item)) return false;

        bool rotated = grid.TryRotate(item);
        if (notify) OnInventoryChanged?.Invoke();
        return rotated;
    }

    public bool CanPlace(ItemInstance item, int x, int y, bool ignoreCurrentItem = false)
    {
        return grid.CanPlace(item, x, y, ignoreCurrentItem ? item : null);
    }

    public bool RemoveItem(ItemInstance item, bool notify = true)
    {
        if (!items.Remove(item)) return false;

        grid.Remove(item);
        if (notify) OnInventoryChanged?.Invoke();
        return true;
    }

    public bool DiscardItem(ItemInstance item)
    {
        return RemoveItem(item);
    }

    public bool TryAddInstance(ItemInstance item, int x, int y, bool notify = true)
    {
        if (item == null || items.Contains(item)) return false;
        if (!grid.Place(item, x, y)) return false;

        items.Add(item);
        if (notify) OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryAddInstanceToFirstSpace(ItemInstance item, bool notify = true)
    {
        if (item == null || items.Contains(item)) return false;
        if (!grid.TryFindSpace(item, out Vector2Int position)) return false;
        return TryAddInstance(item, position.x, position.y, notify);
    }

    public void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    public void Clear()
    {
        grid = new InventoryGrid(gridWidth, gridHeight);
        items.Clear();
        OnInventoryChanged?.Invoke();
    }

    public void AddLoadedItem(
        ItemData itemData,
        int x,
        int y,
        bool rotated,
        string createdByRecipeId = null,
        int createdByRecipeRotation = 0)
    {
        if (itemData == null) return;

        ItemInstance item = new ItemInstance(itemData, createdByRecipeId, createdByRecipeRotation)
        {
            rotated = rotated
        };

        TryAddInstance(item, x, y, false);
        OnInventoryChanged?.Invoke();
    }
}
