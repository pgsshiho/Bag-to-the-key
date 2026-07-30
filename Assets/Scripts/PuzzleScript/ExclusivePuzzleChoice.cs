using System.Collections.Generic;
using UnityEngine;

public class ExclusivePuzzleChoice : MonoBehaviour
{
    [SerializeField] private string choicePuzzleId;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private List<ExclusivePuzzleChoiceOption> options =
        new List<ExclusivePuzzleChoiceOption>();

    public string ChoicePuzzleId => choicePuzzleId;

    private void Awake()
    {
        ResolveOptions();
        ResolveInventory();
    }

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += RefreshOptionStates;
        RefreshOptionStates();
    }

    private void Start()
    {
        RefreshOptionStates();
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= RefreshOptionStates;
    }

    public bool TryChoose(ExclusivePuzzleChoiceOption option)
    {
        if (option == null || !options.Contains(option))
            return false;

        ExclusivePuzzleChoiceOption selected = GetSelectedOption();
        if (selected != null)
        {
            if (selected == option)
            {
                option.ApplySelectedState();
                return true;
            }

            option.NotifyUnavailable();
            return false;
        }

        string outcomeId = option.ResolveOutcomeId(choicePuzzleId);
        if (GameProgressState.IsPuzzleCompleted(choicePuzzleId)
            || GameProgressState.HasOutcome(outcomeId))
        {
            option.NotifyUnavailable();
            return false;
        }

        ResolveInventory();
        if (option.RequiredEquippedItem != null
            && (inventoryManager == null
                || !inventoryManager.IsEquipped(option.RequiredEquippedItem)))
        {
            option.NotifyMissingItem();
            return false;
        }

        if (inventoryManager == null
            || !inventoryManager.TryAddItems(
                option.RewardItems,
                out List<ItemInstance> addedItems,
                false))
        {
            option.NotifyInventoryFull();
            return false;
        }

        if (option.ConsumeRequiredItem
            && option.RequiredEquippedItem != null
            && !inventoryManager.ConsumeEquippedItem(option.RequiredEquippedItem))
        {
            RollbackRewards(addedItems);
            option.NotifyMissingItem();
            return false;
        }

        bool recorded = GameProgressState.RecordOutcome(
            choicePuzzleId,
            outcomeId,
            option.MoralityDelta);
        if (!recorded)
        {
            RollbackRewards(addedItems);
            option.NotifyUnavailable();
            return false;
        }

        if (!option.ConsumeRequiredItem && addedItems.Count > 0)
            inventoryManager.NotifyChanged();

        DiscoveryManager discovery = DiscoveryManager.GetOrCreate();
        foreach (ItemInstance item in addedItems)
            discovery.DiscoverItem(item.data);

        option.NotifyFirstSelected();
        RefreshOptionStates();
        return true;
    }

    private ExclusivePuzzleChoiceOption GetSelectedOption()
    {
        foreach (ExclusivePuzzleChoiceOption option in options)
        {
            if (option != null
                && GameProgressState.HasOutcome(option.ResolveOutcomeId(choicePuzzleId)))
            {
                return option;
            }
        }

        return null;
    }

    private void RefreshOptionStates()
    {
        ResolveOptions();
        ExclusivePuzzleChoiceOption selected = GetSelectedOption();
        foreach (ExclusivePuzzleChoiceOption option in options)
        {
            if (option != null)
                option.SetChoiceState(selected);
        }
    }

    private void ResolveOptions()
    {
        options.RemoveAll(option => option == null);
        if (options.Count > 0) return;
        options.AddRange(GetComponentsInChildren<ExclusivePuzzleChoiceOption>(true));
    }

    private void ResolveInventory()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();
    }

    private void RollbackRewards(IEnumerable<ItemInstance> addedItems)
    {
        if (inventoryManager == null) return;
        foreach (ItemInstance item in addedItems)
            inventoryManager.RemoveItem(item, false);
        inventoryManager.NotifyChanged();
    }
}
