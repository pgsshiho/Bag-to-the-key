using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    public ItemData GetItemById(string itemId)
    {
        foreach (ItemData item in items)
        {
            if (item.itemId == itemId)
                return item;
        }

        return null;
    }
}