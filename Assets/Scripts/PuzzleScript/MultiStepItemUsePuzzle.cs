using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ItemUsePuzzleStep
{
    public string stepId;
    public ItemData requiredEquippedItem;
    public bool consumeOnUse = true;
    public List<ItemData> rewardItems = new List<ItemData>();
    public UnityEvent onStateApplied;
    public UnityEvent onFirstCompleted;
    public UnityEvent onMissingItem;
    public UnityEvent onInventoryFull;
}

[RequireComponent(typeof(PuzzleStateController))]
public class MultiStepItemUsePuzzle : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private List<ItemUsePuzzleStep> steps =
        new List<ItemUsePuzzleStep>();
    [SerializeField] private UnityEvent onAlreadyCompleted;

    private readonly HashSet<int> appliedStepStates = new HashSet<int>();
    private PuzzleStateController completion;

    private void Awake()
    {
        completion = GetComponent<PuzzleStateController>();
        ResolveInventory();
    }

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += RefreshStepStates;
        RefreshStepStates();
    }

    private void Start()
    {
        RefreshStepStates();
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= RefreshStepStates;
    }

    public void Interact()
    {
        if (completion.IsCompleted)
        {
            onAlreadyCompleted?.Invoke();
            return;
        }

        int stepIndex = GetCurrentStepIndex();
        if (stepIndex < 0)
        {
            completion.Complete();
            return;
        }

        ItemUsePuzzleStep step = steps[stepIndex];
        ResolveInventory();
        if (inventoryManager == null)
        {
            step.onMissingItem?.Invoke();
            return;
        }

        if (step.requiredEquippedItem != null
            && !inventoryManager.IsEquipped(step.requiredEquippedItem))
        {
            step.onMissingItem?.Invoke();
            return;
        }

        if (!inventoryManager.TryAddItems(
                step.rewardItems,
                out List<ItemInstance> addedItems,
                false))
        {
            step.onInventoryFull?.Invoke();
            return;
        }

        if (step.consumeOnUse
            && step.requiredEquippedItem != null
            && !inventoryManager.ConsumeEquippedItem(step.requiredEquippedItem))
        {
            RollbackRewards(addedItems);
            step.onMissingItem?.Invoke();
            return;
        }

        if (!step.consumeOnUse && addedItems.Count > 0)
            inventoryManager.NotifyChanged();

        DiscoveryManager discovery = DiscoveryManager.GetOrCreate();
        foreach (ItemInstance item in addedItems)
            discovery.DiscoverItem(item.data);

        GameProgressState.CompletePuzzle(GetStepStateId(step, stepIndex));
        step.onFirstCompleted?.Invoke();
        RefreshStepStates();

        if (GetCurrentStepIndex() < 0)
            completion.Complete();
    }

    private int GetCurrentStepIndex()
    {
        if (steps.Count == 0) return -1;

        for (int i = 0; i < steps.Count; i++)
        {
            if (!GameProgressState.IsPuzzleCompleted(GetStepStateId(steps[i], i)))
                return i;
        }

        return -1;
    }

    private void RefreshStepStates()
    {
        if (completion == null)
            completion = GetComponent<PuzzleStateController>();

        for (int i = 0; i < steps.Count; i++)
        {
            bool completed = completion.IsCompleted
                || GameProgressState.IsPuzzleCompleted(GetStepStateId(steps[i], i));
            if (!completed)
            {
                appliedStepStates.Remove(i);
                continue;
            }

            if (appliedStepStates.Add(i))
                steps[i].onStateApplied?.Invoke();
        }
    }

    private string GetStepStateId(ItemUsePuzzleStep step, int index)
    {
        string stepId = !string.IsNullOrWhiteSpace(step?.stepId)
            ? step.stepId
            : index.ToString();
        return $"{completion.PuzzleId}.step.{stepId}";
    }

    private void RollbackRewards(IEnumerable<ItemInstance> addedItems)
    {
        foreach (ItemInstance item in addedItems)
            inventoryManager.RemoveItem(item, false);
        inventoryManager.NotifyChanged();
    }

    private void ResolveInventory()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();
    }
}
