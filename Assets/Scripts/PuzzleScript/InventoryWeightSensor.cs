using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PuzzleStateController))]
public class InventoryWeightSensor : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private bool includeEquippedItem = true;
    [SerializeField] private int maximumItemCount = -1;
    [SerializeField] private int maximumOccupiedCells = -1;
    [SerializeField] private List<ItemData> itemsThatMustBeAbsent =
        new List<ItemData>();
    [SerializeField] private List<string> requiredPuzzleIds =
        new List<string>();
    [SerializeField] private bool evaluateAutomatically;
    [SerializeField] private UnityEvent onRequirementsMet;
    [SerializeField] private UnityEvent onRequirementsNotMet;

    private PuzzleStateController completion;
    private InventoryManager subscribedInventory;

    private void Awake()
    {
        completion = GetComponent<PuzzleStateController>();
    }

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += HandleStateChanged;
        BindInventory();
        if (evaluateAutomatically)
            Evaluate(false);
    }

    private void Start()
    {
        BindInventory();
        if (evaluateAutomatically)
            Evaluate(false);
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= HandleStateChanged;
        BindInventory(null);
    }

    public void Interact()
    {
        Evaluate(true);
    }

    public bool IsSatisfied()
    {
        BindInventory();
        if (inventoryManager == null)
            return false;

        if (maximumItemCount >= 0)
        {
            int itemCount = inventoryManager.items.Count;
            if (includeEquippedItem && inventoryManager.EquippedItem != null)
                itemCount++;
            if (itemCount > maximumItemCount)
                return false;
        }

        if (maximumOccupiedCells >= 0
            && inventoryManager.GetOccupiedCellCount(includeEquippedItem)
                > maximumOccupiedCells)
        {
            return false;
        }

        foreach (ItemData item in itemsThatMustBeAbsent)
        {
            if (item != null && inventoryManager.ContainsItem(item, includeEquippedItem))
                return false;
        }

        foreach (string puzzleId in requiredPuzzleIds)
        {
            if (!GameProgressState.IsPuzzleCompleted(puzzleId))
                return false;
        }

        return true;
    }

    private void Evaluate(bool notifyFailure)
    {
        if (completion.IsCompleted)
            return;

        if (!IsSatisfied())
        {
            if (notifyFailure)
                onRequirementsNotMet?.Invoke();
            return;
        }

        completion.Complete();
        onRequirementsMet?.Invoke();
    }

    private void HandleStateChanged()
    {
        if (evaluateAutomatically)
            Evaluate(false);
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
            subscribedInventory.OnInventoryChanged -= HandleInventoryChanged;

        subscribedInventory = manager;
        inventoryManager = manager;
        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged += HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        if (evaluateAutomatically)
            Evaluate(false);
    }
}
