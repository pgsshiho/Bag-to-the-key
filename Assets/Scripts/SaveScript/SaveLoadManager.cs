using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveLoadManager : MonoBehaviour
{
    private static SaveLoadManager instance;

    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private Camera targetCamera;

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) SaveGame();
        if (Input.GetKeyDown(KeyCode.F9)) LoadGame();
    }

    public void SaveGame()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();

        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManager가 없어 저장할 수 없습니다.");
            return;
        }

        SaveData data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name
        };

        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null)
        {
            data.cameraPosition = new Vector3Data(targetCamera.transform.position);
            data.cameraRotation = new Vector3Data(targetCamera.transform.eulerAngles);
        }

        foreach (ItemInstance item in inventoryManager.items)
        {
            data.inventoryItems.Add(new InventoryItemSaveData
            {
                itemId = item.data.itemId,
                x = item.x,
                y = item.y,
                rotated = item.rotated,
                createdByRecipeId = item.createdByRecipeId,
                createdByRecipeRotation = item.createdByRecipeRotation
            });
        }

        DiscoveryManager discovery = DiscoveryManager.GetOrCreate();
        data.discoveredItemIds.AddRange(discovery.DiscoveredItemIds);
        data.discoveredRecipeIds.AddRange(discovery.DiscoveredRecipeIds);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"저장 완료: {SavePath}");
    }

    public void LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("저장 파일이 없습니다.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null || string.IsNullOrWhiteSpace(data.sceneName))
        {
            Debug.LogWarning("저장 파일이 손상됐습니다.");
            return;
        }

        StartCoroutine(LoadGameRoutine(data));
    }

    private IEnumerator LoadGameRoutine(SaveData data)
    {
        if (SceneManager.GetActiveScene().name != data.sceneName)
        {
            yield return SceneManager.LoadSceneAsync(data.sceneName);
            yield return null;
        }

        inventoryManager = FindAnyObjectByType<InventoryManager>();
        targetCamera = Camera.main;

        if (targetCamera != null && data.cameraPosition != null && data.cameraRotation != null)
        {
            targetCamera.transform.position = data.cameraPosition.ToVector3();
            targetCamera.transform.eulerAngles = data.cameraRotation.ToVector3();
        }

        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManager를 찾을 수 없어 로드를 중단합니다.");
            yield break;
        }

        inventoryManager.Clear();
        if (data.inventoryItems == null)
            data.inventoryItems = new System.Collections.Generic.List<InventoryItemSaveData>();

        foreach (InventoryItemSaveData savedItem in data.inventoryItems)
        {
            ItemData itemData = ResolveItem(savedItem.itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"ItemData를 찾을 수 없습니다: {savedItem.itemId}");
                continue;
            }

            inventoryManager.AddLoadedItem(
                itemData,
                savedItem.x,
                savedItem.y,
                savedItem.rotated,
                savedItem.createdByRecipeId,
                savedItem.createdByRecipeRotation);
        }

        DiscoveryManager.GetOrCreate().Restore(
            data.discoveredItemIds ?? new System.Collections.Generic.List<string>(),
            data.discoveredRecipeIds ?? new System.Collections.Generic.List<string>());
        Debug.Log("로드 완료");
    }

    private ItemData ResolveItem(string itemId)
    {
        if (itemDatabase != null)
        {
            ItemData databaseItem = itemDatabase.GetItemById(itemId);
            if (databaseItem != null) return databaseItem;
        }

        foreach (ItemData item in Resources.LoadAll<ItemData>(string.Empty))
        {
            if (item.itemId == itemId) return item;
        }

        return null;
    }
}
