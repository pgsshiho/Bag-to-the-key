using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Itemgetbase : MonoBehaviour, IWorldInteractable
{
    public ItemData item;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private string persistentPickupId;
    [SerializeField] private bool keepForStateRestore;

    private bool isPickingUp;
    private string resolvedPickupId;

    private void Awake()
    {
        if (GetComponent<Collider>() == null && GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider>();

        resolvedPickupId = ResolvePersistentPickupId();
        ResolveInventory();
    }

    private void OnEnable()
    {
        isPickingUp = false;
        GameProgressState.ProgressChanged += RefreshCollectedState;
        RefreshCollectedState();
    }

    private void Start()
    {
        RefreshCollectedState();
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= RefreshCollectedState;
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
        GameProgressState.CompletePuzzle(resolvedPickupId);
        gameObject.SetActive(false);
        if (!keepForStateRestore) Destroy(gameObject);
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

    private void RefreshCollectedState()
    {
        if (string.IsNullOrWhiteSpace(resolvedPickupId))
            resolvedPickupId = ResolvePersistentPickupId();

        if (GameProgressState.IsPuzzleCompleted(resolvedPickupId))
            gameObject.SetActive(false);
    }

    private string ResolvePersistentPickupId()
    {
        if (!string.IsNullOrWhiteSpace(persistentPickupId))
            return persistentPickupId;

        StringBuilder path = new StringBuilder(name);
        Transform current = transform.parent;
        while (current != null)
        {
            path.Insert(0, '/');
            path.Insert(0, current.name);
            current = current.parent;
        }

        return $"pickup:{SceneManager.GetActiveScene().name}:{path}";
    }
}
