using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class InventoryLayoutRequirement
{
    public ItemData item;
    public Vector2Int position;
    public bool rotated;
}

[RequireComponent(typeof(PuzzleStateController))]
public class InventoryLayoutPuzzle : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private List<InventoryLayoutRequirement> requirements =
        new List<InventoryLayoutRequirement>();
    [SerializeField] private bool requireOnlyListedItems;
    [SerializeField] private UnityEvent onLayoutMatched;

    private readonly HashSet<ItemInstance> matchedItems = new HashSet<ItemInstance>();
    private PuzzleStateController completion;
    private InventoryManager subscribedInventory;

    private void Awake()
    {
        completion = GetComponent<PuzzleStateController>();
    }

    private void OnEnable()
    {
        BindInventory();
        EvaluateNow();
    }

    private void Start()
    {
        BindInventory();
        EvaluateNow();
    }

    private void OnDisable()
    {
        BindInventory(null);
    }

    public void EvaluateNow()
    {
        if (completion == null)
            completion = GetComponent<PuzzleStateController>();
        if (completion.IsCompleted || requirements.Count == 0)
            return;

        BindInventory();
        if (inventoryManager == null)
            return;
        if (requireOnlyListedItems
            && inventoryManager.items.Count != requirements.Count)
            return;

        matchedItems.Clear();
        foreach (InventoryLayoutRequirement requirement in requirements)
        {
            if (requirement == null || requirement.item == null)
                return;

            ItemInstance match = null;
            foreach (ItemInstance item in inventoryManager.items)
            {
                if (matchedItems.Contains(item)
                    || !MatchesItem(item, requirement.item)
                    || item.x != requirement.position.x
                    || item.y != requirement.position.y
                    || item.rotated != requirement.rotated)
                {
                    continue;
                }

                match = item;
                break;
            }

            if (match == null)
                return;
            matchedItems.Add(match);
        }

        completion.Complete();
        onLayoutMatched?.Invoke();
    }

    private void BindInventory()
    {
        InventoryManager resolved = inventoryManager != null
            ? inventoryManager
            : FindAnyObjectByType<InventoryManager>();
        BindInventory(resolved);
    }

    private void BindInventory(InventoryManager manager)
    {
        if (subscribedInventory == manager)
        {
            inventoryManager = manager;
            return;
        }

        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged -= EvaluateNow;

        subscribedInventory = manager;
        inventoryManager = manager;
        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged += EvaluateNow;
    }

    private static bool MatchesItem(ItemInstance instance, ItemData itemData)
    {
        if (instance?.data == null || itemData == null) return false;
        if (instance.data == itemData) return true;
        return !string.IsNullOrWhiteSpace(itemData.itemId)
            && instance.data.itemId == itemData.itemId;
    }
}
