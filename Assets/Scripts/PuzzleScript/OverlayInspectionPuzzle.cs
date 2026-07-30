using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PuzzleStateController))]
public class OverlayInspectionPuzzle : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private ItemData baseItem;
    [SerializeField] private ItemData overlayItem;
    [SerializeField] private Sprite baseSpriteOverride;
    [SerializeField] private Sprite overlaySpriteOverride;
    [SerializeField] private string displayTitle = "겹쳐 보기";
    [SerializeField, TextArea] private string revealedMessage;
    [SerializeField] private bool requireBaseItemEquipped;
    [SerializeField] private bool consumeItemsOnComplete;
    [SerializeField] private bool allowReviewAfterCompletion = true;
    [SerializeField] private UnityEvent onMissingItems;
    [SerializeField] private UnityEvent onInspected;

    private PuzzleStateController completion;

    private void Awake()
    {
        completion = GetComponent<PuzzleStateController>();
        ResolveInventory();
    }

    public void Interact()
    {
        ResolveInventory();
        if (inventoryManager == null || !HasRequiredItems())
        {
            onMissingItems?.Invoke();
            return;
        }

        if (completion.IsCompleted && !allowReviewAfterCompletion)
            return;

        Sprite baseSprite = baseSpriteOverride != null
            ? baseSpriteOverride
            : baseItem?.icon;
        Sprite overlaySprite = overlaySpriteOverride != null
            ? overlaySpriteOverride
            : overlayItem?.icon;

        PuzzleModalUI.GetOrCreate().ShowOverlay(
            displayTitle,
            baseSprite,
            overlaySprite,
            revealedMessage,
            CompleteInspection);
    }

    private void CompleteInspection()
    {
        if (completion.IsCompleted)
        {
            onInspected?.Invoke();
            return;
        }

        if (consumeItemsOnComplete)
        {
            List<ItemData> consumedItems = new List<ItemData>
            {
                baseItem,
                overlayItem
            };
            if (!inventoryManager.TryConsumeItems(consumedItems))
            {
                onMissingItems?.Invoke();
                return;
            }
        }

        completion.Complete();
        onInspected?.Invoke();
    }

    private bool HasRequiredItems()
    {
        if (baseItem == null || overlayItem == null) return false;
        if (requireBaseItemEquipped && !inventoryManager.IsEquipped(baseItem))
            return false;

        if (baseItem == overlayItem
            || (!string.IsNullOrWhiteSpace(baseItem.itemId)
                && baseItem.itemId == overlayItem.itemId))
        {
            return inventoryManager.GetItemCount(baseItem) >= 2;
        }

        return inventoryManager.ContainsItem(baseItem)
            && inventoryManager.ContainsItem(overlayItem);
    }

    private void ResolveInventory()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();
    }
}
