using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public event Action OnInventoryChanged;
    public event Action<ItemInstance> OnEquippedItemChanged;

    [Header("Inventory Size")]
    public int gridWidth = 10;
    public int gridHeight = 6;

    public InventoryGrid grid;
    public List<ItemInstance> items = new List<ItemInstance>();
    private ItemInstance equippedItem;

    public ItemInstance EquippedItem => equippedItem;

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

    public bool TryAddItems(
        IEnumerable<ItemData> itemDataCollection,
        out List<ItemInstance> addedItems,
        bool notify = true)
    {
        addedItems = new List<ItemInstance>();
        if (itemDataCollection == null) return true;

        foreach (ItemData itemData in itemDataCollection)
        {
            if (itemData == null)
            {
                RollbackAddedItems(addedItems);
                addedItems.Clear();
                return false;
            }

            ItemInstance item = new ItemInstance(itemData);
            if (!grid.TryFindSpace(item, out Vector2Int position)
                || !grid.Place(item, position.x, position.y))
            {
                RollbackAddedItems(addedItems);
                addedItems.Clear();
                return false;
            }

            items.Add(item);
            addedItems.Add(item);
        }

        if (notify && addedItems.Count > 0)
            OnInventoryChanged?.Invoke();
        return true;
    }

    public bool ContainsItem(ItemData itemData, bool includeEquipped = true)
    {
        return itemData != null
            && GetItemCount(itemData, includeEquipped) > 0;
    }

    public int GetItemCount(ItemData itemData, bool includeEquipped = true)
    {
        if (itemData == null) return 0;

        int count = 0;
        foreach (ItemInstance item in items)
        {
            if (Matches(item, itemData))
                count++;
        }

        if (includeEquipped && Matches(equippedItem, itemData))
            count++;
        return count;
    }

    public int GetOccupiedCellCount(bool includeEquipped = true)
    {
        int occupiedCells = 0;
        foreach (ItemInstance item in items)
        {
            if (item?.data != null)
                occupiedCells += item.Width * item.Height;
        }

        if (includeEquipped && equippedItem?.data != null)
            occupiedCells += equippedItem.Width * equippedItem.Height;
        return occupiedCells;
    }

    public bool TryConsumeItems(
        IReadOnlyList<ItemData> requiredItems,
        bool includeEquipped = true)
    {
        if (requiredItems == null || requiredItems.Count == 0)
            return true;

        List<ItemInstance> matchedItems = new List<ItemInstance>();
        bool consumesEquippedItem = false;

        foreach (ItemData requiredItem in requiredItems)
        {
            if (requiredItem == null) return false;

            ItemInstance match = null;
            foreach (ItemInstance item in items)
            {
                if (matchedItems.Contains(item) || !Matches(item, requiredItem))
                    continue;

                match = item;
                break;
            }

            if (match == null
                && includeEquipped
                && !consumesEquippedItem
                && Matches(equippedItem, requiredItem))
            {
                match = equippedItem;
                consumesEquippedItem = true;
            }

            if (match == null)
                return false;
            matchedItems.Add(match);
        }

        foreach (ItemInstance item in matchedItems)
        {
            if (item == equippedItem)
            {
                equippedItem = null;
                continue;
            }

            items.Remove(item);
            grid.Remove(item);
        }

        if (consumesEquippedItem)
            OnEquippedItemChanged?.Invoke(null);
        OnInventoryChanged?.Invoke();
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

    public bool EquipDetachedItem(ItemInstance item)
    {
        if (item == null || equippedItem != null || !items.Contains(item))
            return false;

        grid.Remove(item);
        items.Remove(item);
        item.x = -1;
        item.y = -1;
        equippedItem = item;
        NotifyEquippedItemChanged();
        return true;
    }

    public bool TryEquipItem(ItemInstance item)
    {
        if (item == null || equippedItem != null || !items.Contains(item))
            return false;

        grid.Remove(item);
        return EquipDetachedItem(item);
    }

    public bool TryUnequipItem()
    {
        if (equippedItem == null) return false;
        if (!grid.TryFindSpace(equippedItem, out Vector2Int position))
            return false;

        ItemInstance item = equippedItem;
        if (!grid.Place(item, position.x, position.y))
            return false;

        equippedItem = null;
        items.Add(item);
        NotifyEquippedItemChanged();
        return true;
    }

    public bool IsEquipped(ItemData itemData)
    {
        return itemData != null && IsEquipped(itemData.itemId);
    }

    public bool IsEquipped(string itemId)
    {
        return equippedItem?.data != null
            && !string.IsNullOrWhiteSpace(itemId)
            && equippedItem.data.itemId == itemId;
    }

    public bool ConsumeEquippedItem(ItemData expectedItem = null)
    {
        if (equippedItem == null) return false;
        if (expectedItem != null && !IsEquipped(expectedItem))
            return false;

        equippedItem = null;
        NotifyEquippedItemChanged();
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
        if (item != null && item == equippedItem)
        {
            equippedItem = null;
            if (notify) NotifyEquippedItemChanged();
            return true;
        }

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
        equippedItem = null;
        OnEquippedItemChanged?.Invoke(null);
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

    public void AddLoadedEquippedItem(
        ItemData itemData,
        bool rotated,
        string createdByRecipeId = null,
        int createdByRecipeRotation = 0)
    {
        if (itemData == null || equippedItem != null) return;

        equippedItem = new ItemInstance(
            itemData,
            createdByRecipeId,
            createdByRecipeRotation)
        {
            rotated = rotated,
            x = -1,
            y = -1
        };

        NotifyEquippedItemChanged();
    }

    private void NotifyEquippedItemChanged()
    {
        OnEquippedItemChanged?.Invoke(equippedItem);
        OnInventoryChanged?.Invoke();
    }

    private void RollbackAddedItems(IEnumerable<ItemInstance> addedItems)
    {
        foreach (ItemInstance item in addedItems)
        {
            items.Remove(item);
            grid.Remove(item);
        }
    }

    private static bool Matches(ItemInstance instance, ItemData itemData)
    {
        if (instance?.data == null || itemData == null) return false;
        if (instance.data == itemData) return true;

        return !string.IsNullOrWhiteSpace(itemData.itemId)
            && instance.data.itemId == itemData.itemId;
    }
}
