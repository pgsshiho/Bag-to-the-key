using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InventoryItemReward : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private List<ItemData> rewardItems = new List<ItemData>();
    [SerializeField] private UnityEvent onGranted;
    [SerializeField] private UnityEvent onInventoryFull;

    public bool Grant()
    {
        ResolveInventory();
        if (inventoryManager == null)
        {
            Debug.LogWarning($"{name}: InventoryManager를 찾을 수 없습니다.", this);
            return false;
        }

        if (!inventoryManager.TryAddItems(rewardItems, out List<ItemInstance> addedItems))
        {
            onInventoryFull?.Invoke();
            return false;
        }

        DiscoveryManager discovery = DiscoveryManager.GetOrCreate();
        foreach (ItemInstance item in addedItems)
            discovery.DiscoverItem(item.data);

        onGranted?.Invoke();
        return true;
    }

    private void Awake()
    {
        ResolveInventory();
    }

    private void ResolveInventory()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();
    }
}
