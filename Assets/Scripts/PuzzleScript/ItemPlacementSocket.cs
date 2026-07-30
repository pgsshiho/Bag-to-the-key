using UnityEngine;
using UnityEngine.Events;

public class ItemPlacementSocket : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private ItemPlacementPuzzle puzzle;
    [SerializeField] private string socketId;
    [SerializeField] private ItemData requiredItem;
    [SerializeField, Min(0)] private int sequenceIndex;
    [SerializeField] private bool consumeOnPlace = true;
    [SerializeField] private GameObject placedVisual;
    [SerializeField] private UnityEvent onFilledStateApplied;
    [SerializeField] private UnityEvent onFirstPlaced;
    [SerializeField] private UnityEvent onWrongItem;
    [SerializeField] private UnityEvent onWrongOrder;

    private bool filledStateApplied;

    public string SocketId => socketId;
    public ItemData RequiredItem => requiredItem;
    public int SequenceIndex => sequenceIndex;
    public bool ConsumeOnPlace => consumeOnPlace;

    private void Awake()
    {
        if (puzzle == null)
            puzzle = GetComponentInParent<ItemPlacementPuzzle>();
    }

    public void Interact()
    {
        if (puzzle == null)
            puzzle = GetComponentInParent<ItemPlacementPuzzle>();
        puzzle?.TryPlace(this);
    }

    public void SetFilledState(bool filled)
    {
        if (placedVisual != null)
            placedVisual.SetActive(filled);

        if (!filled)
        {
            filledStateApplied = false;
            return;
        }

        ApplyFilledState();
    }

    public void ApplyFilledState()
    {
        if (filledStateApplied) return;
        filledStateApplied = true;
        onFilledStateApplied?.Invoke();
    }

    public void NotifyFirstPlaced()
    {
        onFirstPlaced?.Invoke();
    }

    public void NotifyWrongItem()
    {
        onWrongItem?.Invoke();
    }

    public void NotifyWrongOrder()
    {
        onWrongOrder?.Invoke();
    }
}
