using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveLoadManager : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private Camera targetCamera;

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
            SaveGame();

        if (Input.GetKeyDown(KeyCode.F9))
            LoadGame();
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();

        data.sceneName = SceneManager.GetActiveScene().name;

        if (targetCamera != null)
        {
            data.cameraPosition = new Vector3Data(targetCamera.transform.position);
            data.cameraRotation = new Vector3Data(targetCamera.transform.eulerAngles);
        }

        foreach (ItemInstance item in inventoryManager.items)
        {
            InventoryItemSaveData itemData = new InventoryItemSaveData();
            itemData.itemId = item.data.itemId;
            itemData.x = item.x;
            itemData.y = item.y;
            itemData.rotated = item.rotated;

            data.inventoryItems.Add(itemData);
        }

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

        StartCoroutine(LoadGameRoutine(data));
    }

    private IEnumerator LoadGameRoutine(SaveData data)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName != data.sceneName)
        {
            yield return SceneManager.LoadSceneAsync(data.sceneName);
            yield return null;
        }

        inventoryManager = FindObjectOfType<InventoryManager>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            targetCamera.transform.position = data.cameraPosition.ToVector3();
            targetCamera.transform.eulerAngles = data.cameraRotation.ToVector3();
        }

        inventoryManager.Clear();

        foreach (InventoryItemSaveData savedItem in data.inventoryItems)
        {
            ItemData itemData = itemDatabase.GetItemById(savedItem.itemId);

            if (itemData == null)
            {
                Debug.LogWarning($"ItemData를 찾을 수 없습니다: {savedItem.itemId}");
                continue;
            }

            inventoryManager.AddLoadedItem(
                itemData,
                savedItem.x,
                savedItem.y,
                savedItem.rotated
            );
        }

        Debug.Log("로드 완료");
    }
}