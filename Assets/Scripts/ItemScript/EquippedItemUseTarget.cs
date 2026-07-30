using UnityEngine;
using UnityEngine.Events;

public class EquippedItemUseTarget : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private ItemData requiredItem;
    [SerializeField] private bool consumeOnUse;
    [SerializeField] private bool allowRepeatedUse;
    [SerializeField] private string puzzleId;
    [SerializeField] private UnityEvent onUsed;
    [SerializeField] private UnityEvent onUsedStateApplied;
    [SerializeField] private UnityEvent onMissingRequiredItem;

    private bool hasBeenUsed;
    private bool usedStateApplied;

    public ItemData RequiredItem => requiredItem;
    public bool HasBeenUsed => hasBeenUsed
        || GameProgressState.IsPuzzleCompleted(puzzleId);

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += RefreshUsedState;
        RefreshUsedState();
    }

    private void Start()
    {
        RefreshUsedState();
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= RefreshUsedState;
    }

    public void Interact()
    {
        if (requiredItem == null)
        {
            Debug.LogWarning($"{name}: Required Item이 지정되지 않았습니다.", this);
            return;
        }

        ResolveInventoryManager();
        if (inventoryManager == null)
        {
            Debug.LogWarning($"{name}: InventoryManager를 찾을 수 없습니다.", this);
            return;
        }

        if (HasBeenUsed && !allowRepeatedUse)
            return;

        if (!inventoryManager.IsEquipped(requiredItem))
        {
            onMissingRequiredItem?.Invoke();
            return;
        }

        hasBeenUsed = true;
        onUsed?.Invoke();

        if (consumeOnUse)
            inventoryManager.ConsumeEquippedItem(requiredItem);

        if (!string.IsNullOrWhiteSpace(puzzleId))
            GameProgressState.CompletePuzzle(puzzleId);
        RefreshUsedState();
    }

    private void Awake()
    {
        ResolveInventoryManager();
    }

    private void ResolveInventoryManager()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();
    }

    private void RefreshUsedState()
    {
        bool used = hasBeenUsed
            || GameProgressState.IsPuzzleCompleted(puzzleId);
        if (!used)
        {
            usedStateApplied = false;
            return;
        }

        hasBeenUsed = true;
        if (usedStateApplied) return;
        usedStateApplied = true;
        onUsedStateApplied?.Invoke();
    }
}
