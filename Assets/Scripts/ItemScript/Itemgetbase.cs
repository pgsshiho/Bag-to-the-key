using UnityEngine;

public class Itemgetbase : MonoBehaviour, IWorldInteractable
{
    public ItemData item;
    [SerializeField] private InventoryManager inventoryManager;

    private bool isPickingUp;

    private void Awake()
    {
        if (GetComponent<Collider>() == null && GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider>();

        ResolveInventory();
    }

    public void Interact()
    {
        TryPickup();
    }

    public bool TryPickup()
    {
        if (isPickingUp || item == null) return false;
        if (inventoryManager == null) ResolveInventory();
        if (inventoryManager == null)
        {
            Debug.LogWarning($"{name}: InventoryManager를 찾을 수 없습니다.");
            return false;
        }

        isPickingUp = true;
        bool added = inventoryManager.AddItem(item);
        if (!added)
        {
            isPickingUp = false;
            return false;
        }

        DiscoveryManager.GetOrCreate().DiscoverItem(item);
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }

    public void GetItem()
    {
        TryPickup();
    }

    private void ResolveInventory()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();
    }
}
