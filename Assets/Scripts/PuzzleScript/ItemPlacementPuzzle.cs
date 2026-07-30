using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PuzzleStateController))]
public class ItemPlacementPuzzle : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private bool requireSequence = true;
    [SerializeField] private List<ItemPlacementSocket> sockets =
        new List<ItemPlacementSocket>();
    [SerializeField] private UnityEvent onWrongItem;
    [SerializeField] private UnityEvent onWrongOrder;

    private PuzzleStateController completion;

    private void Awake()
    {
        completion = GetComponent<PuzzleStateController>();
        ResolveSockets();
        ResolveInventory();
    }

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += RefreshState;
        RefreshState();
    }

    private void Start()
    {
        RefreshState();
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= RefreshState;
    }

    public bool TryPlace(ItemPlacementSocket socket)
    {
        if (socket == null || !sockets.Contains(socket))
            return false;

        if (IsSocketFilled(socket))
        {
            socket.ApplyFilledState();
            return true;
        }

        if (requireSequence && socket.SequenceIndex != GetNextSequenceIndex())
        {
            onWrongOrder?.Invoke();
            socket.NotifyWrongOrder();
            return false;
        }

        ResolveInventory();
        if (inventoryManager == null
            || socket.RequiredItem == null
            || !inventoryManager.IsEquipped(socket.RequiredItem))
        {
            onWrongItem?.Invoke();
            socket.NotifyWrongItem();
            return false;
        }

        if (socket.ConsumeOnPlace
            && !inventoryManager.ConsumeEquippedItem(socket.RequiredItem))
        {
            onWrongItem?.Invoke();
            socket.NotifyWrongItem();
            return false;
        }

        GameProgressState.CompletePuzzle(GetSocketStateId(socket));
        socket.NotifyFirstPlaced();
        RefreshState();

        if (AreAllSocketsFilled())
            completion.Complete();
        return true;
    }

    public bool IsSocketFilled(ItemPlacementSocket socket)
    {
        if (socket == null || completion == null) return false;
        return completion.IsCompleted
            || GameProgressState.IsPuzzleCompleted(GetSocketStateId(socket));
    }

    private void RefreshState()
    {
        ResolveSockets();
        foreach (ItemPlacementSocket socket in sockets)
        {
            if (socket != null)
                socket.SetFilledState(IsSocketFilled(socket));
        }
    }

    private bool AreAllSocketsFilled()
    {
        if (sockets.Count == 0) return false;
        foreach (ItemPlacementSocket socket in sockets)
        {
            if (socket == null || !IsSocketFilled(socket))
                return false;
        }

        return true;
    }

    private int GetNextSequenceIndex()
    {
        int next = int.MaxValue;
        foreach (ItemPlacementSocket socket in sockets)
        {
            if (socket == null || IsSocketFilled(socket)) continue;
            next = Mathf.Min(next, socket.SequenceIndex);
        }

        return next;
    }

    private string GetSocketStateId(ItemPlacementSocket socket)
    {
        string socketId = !string.IsNullOrWhiteSpace(socket.SocketId)
            ? socket.SocketId
            : socket.name;
        return $"{completion.PuzzleId}.socket.{socketId}";
    }

    private void ResolveSockets()
    {
        sockets.RemoveAll(socket => socket == null);
        if (sockets.Count > 0) return;

        sockets.AddRange(GetComponentsInChildren<ItemPlacementSocket>(true));
    }

    private void ResolveInventory()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();
    }
}
