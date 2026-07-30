using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveLoadManager : MonoBehaviour
{
    public const int ManualSlotCount = 5;

    private static SaveLoadManager instance;

    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private Camera targetCamera;
    [SerializeField, Range(1, ManualSlotCount)] private int selectedManualSlot = 1;
    [SerializeField, Min(0f)] private float autoSaveDelay = 0.75f;
    [SerializeField] private string[] autoSaveExcludedScenes = { "Mainmenu" };

    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
    private Coroutine pendingAutoSave;
    private InventoryManager subscribedInventoryManager;
    private bool isLoading;

    public static SaveLoadManager Instance => instance;
    public int SelectedManualSlot => selectedManualSlot;
    public event Action SaveSlotsChanged;

    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");
    private string LegacySavePath => Path.Combine(Application.persistentDataPath, "save.json");
    private string AutoSavePath => Path.Combine(SaveDirectory, "autosave.json");

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstanceAfterFirstSceneLoad()
    {
        GetOrCreate();
    }

    public static SaveLoadManager GetOrCreate()
    {
        if (instance != null) return instance;

        SaveLoadManager existing = FindAnyObjectByType<SaveLoadManager>();
        if (existing != null) return existing;

        GameObject managerObject = new GameObject(nameof(SaveLoadManager));
        return managerObject.AddComponent<SaveLoadManager>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        GameProgressState.ProgressChanged += ScheduleAutoSave;
    }

    private IEnumerator Start()
    {
        yield return null;
        RebindSceneReferences();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        GameProgressState.ProgressChanged -= ScheduleAutoSave;
        SubscribeToInventory(null);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) OpenSaveMenu();
        if (Input.GetKeyDown(KeyCode.F9)) OpenLoadMenu();
    }

    public void OpenSaveMenu()
    {
        SaveSlotMenuController.GetOrCreate().Show(this, SaveSlotMenuMode.Save);
    }

    public void OpenLoadMenu()
    {
        SaveSlotMenuController.GetOrCreate().Show(this, SaveSlotMenuMode.Load);
    }

    public void SaveGame()
    {
        SaveGame(selectedManualSlot);
    }

    public void SaveGame(int slotNumber)
    {
        if (!IsValidManualSlot(slotNumber))
        {
            Debug.LogWarning($"저장 슬롯은 1부터 {ManualSlotCount}까지 선택할 수 있습니다.");
            return;
        }

        selectedManualSlot = slotNumber;
        SaveGameInternal(GetManualSavePath(slotNumber), slotNumber, false);
    }

    public void AutoSaveGame()
    {
        if (isLoading || IsAutoSaveExcludedScene(SceneManager.GetActiveScene().name))
            return;

        SaveGameInternal(AutoSavePath, 0, true);
    }

    private void SaveGameInternal(string path, int slotNumber, bool isAutoSave)
    {
        if (inventoryManager == null)
            RebindSceneReferences();

        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManager가 없어 저장할 수 없습니다.");
            return;
        }

        SaveData data = new SaveData
        {
            slotNumber = slotNumber,
            isAutoSave = isAutoSave,
            savedAtUtc = DateTime.UtcNow.ToString("O"),
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
            data.inventoryItems.Add(CreateInventoryItemSaveData(item));
        }

        if (inventoryManager.EquippedItem != null)
            data.equippedItem = CreateInventoryItemSaveData(inventoryManager.EquippedItem);

        DiscoveryManager discovery = DiscoveryManager.GetOrCreate();
        data.discoveredItemIds.AddRange(discovery.DiscoveredItemIds);
        data.discoveredRecipeIds.AddRange(discovery.DiscoveredRecipeIds);
        data.moralityBalance = GameProgressState.MoralityBalance;
        data.completedPuzzleIds.AddRange(GameProgressState.CompletedPuzzleIds);
        data.recordedOutcomeIds.AddRange(GameProgressState.RecordedOutcomeIds);

        string json = JsonUtility.ToJson(data, true);
        Directory.CreateDirectory(SaveDirectory);
        try
        {
            WriteSaveFile(path, json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"저장 파일을 쓰지 못했습니다: {path}\n{exception.Message}");
            return;
        }

        Debug.Log($"{(isAutoSave ? "자동 저장" : $"{slotNumber}번 슬롯 저장")} 완료: {path}");
        SaveSlotsChanged?.Invoke();
    }

    public void LoadGame()
    {
        LoadGame(selectedManualSlot);
    }

    public void LoadGame(int slotNumber)
    {
        if (!IsValidManualSlot(slotNumber))
        {
            Debug.LogWarning($"불러오기 슬롯은 1부터 {ManualSlotCount}까지 선택할 수 있습니다.");
            return;
        }

        selectedManualSlot = slotNumber;
        string path = GetManualSavePath(slotNumber);
        if (!SaveFileExists(path)
            && slotNumber == 1
            && SaveFileExists(LegacySavePath))
            path = LegacySavePath;

        LoadGameFromPath(path);
    }

    public void LoadAutoSave()
    {
        LoadGameFromPath(AutoSavePath);
    }

    public SaveSlotInfo[] GetSaveSlots()
    {
        SaveSlotInfo[] slots = new SaveSlotInfo[ManualSlotCount + 1];
        for (int slotNumber = 1; slotNumber <= ManualSlotCount; slotNumber++)
        {
            string path = GetManualSavePath(slotNumber);
            if (!SaveFileExists(path)
                && slotNumber == 1
                && SaveFileExists(LegacySavePath))
                path = LegacySavePath;

            slots[slotNumber - 1] = ReadSlotInfo(path, slotNumber, false);
        }

        slots[ManualSlotCount] = ReadSlotInfo(AutoSavePath, 0, true);
        return slots;
    }

    public bool HasManualSave(int slotNumber)
    {
        if (!IsValidManualSlot(slotNumber)) return false;
        return SaveFileExists(GetManualSavePath(slotNumber))
            || (slotNumber == 1 && SaveFileExists(LegacySavePath));
    }

    public bool HasAutoSave()
    {
        return SaveFileExists(AutoSavePath);
    }

    public void SelectManualSlot(int slotNumber)
    {
        if (IsValidManualSlot(slotNumber))
            selectedManualSlot = slotNumber;
    }

    private void LoadGameFromPath(string path)
    {
        if (!SaveFileExists(path))
        {
            Debug.Log("저장 파일이 없습니다.");
            return;
        }

        if (!TryReadSaveData(path, out SaveData data))
        {
            Debug.LogWarning("저장 파일이 손상됐습니다.");
            return;
        }

        isLoading = true;
        CancelPendingAutoSave();
        StartCoroutine(LoadGameRoutine(data));
    }

    private IEnumerator LoadGameRoutine(SaveData data)
    {
        if (SceneManager.GetActiveScene().name != data.sceneName)
        {
            yield return SceneTransitionService
                .GetOrCreate()
                .LoadSceneAndWait(data.sceneName);
            yield return null;

            if (SceneManager.GetActiveScene().name != data.sceneName)
            {
                Debug.LogWarning(
                    $"저장된 장면으로 이동하지 못했습니다: {data.sceneName}");
                isLoading = false;
                yield break;
            }
        }

        inventoryManager = FindAnyObjectByType<InventoryManager>();
        targetCamera = Camera.main;
        SubscribeToInventory(inventoryManager);

        if (targetCamera != null && data.cameraPosition != null && data.cameraRotation != null)
        {
            targetCamera.transform.position = data.cameraPosition.ToVector3();
            targetCamera.transform.eulerAngles = data.cameraRotation.ToVector3();
        }

        GameProgressState.Restore(
            data.moralityBalance,
            data.completedPuzzleIds,
            data.recordedOutcomeIds);

        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManager를 찾을 수 없어 로드를 중단합니다.");
            isLoading = false;
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

        if (data.equippedItem != null)
        {
            ItemData equippedItemData = ResolveItem(data.equippedItem.itemId);
            if (equippedItemData != null)
            {
                inventoryManager.AddLoadedEquippedItem(
                    equippedItemData,
                    data.equippedItem.rotated,
                    data.equippedItem.createdByRecipeId,
                    data.equippedItem.createdByRecipeRotation);
            }
            else
            {
                Debug.LogWarning(
                    $"장착 아이템의 ItemData를 찾을 수 없습니다: {data.equippedItem.itemId}");
            }
        }

        DiscoveryManager.GetOrCreate().Restore(
            data.discoveredItemIds ?? new List<string>(),
            data.discoveredRecipeIds ?? new List<string>());
        isLoading = false;
        Debug.Log("로드 완료");
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindSceneReferences();
        if (!isLoading)
            ScheduleAutoSave();
    }

    private void RebindSceneReferences()
    {
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        targetCamera = Camera.main;
        SubscribeToInventory(inventoryManager);
    }

    private void SubscribeToInventory(InventoryManager manager)
    {
        if (subscribedInventoryManager == manager) return;

        if (subscribedInventoryManager != null)
            subscribedInventoryManager.OnInventoryChanged -= ScheduleAutoSave;

        subscribedInventoryManager = manager;
        if (subscribedInventoryManager != null)
            subscribedInventoryManager.OnInventoryChanged += ScheduleAutoSave;
    }

    private void ScheduleAutoSave()
    {
        if (isLoading || inventoryManager == null
            || IsAutoSaveExcludedScene(SceneManager.GetActiveScene().name))
            return;

        CancelPendingAutoSave();
        pendingAutoSave = StartCoroutine(AutoSaveAfterDelay());
    }

    private IEnumerator AutoSaveAfterDelay()
    {
        if (autoSaveDelay > 0f)
            yield return new WaitForSecondsRealtime(autoSaveDelay);

        pendingAutoSave = null;
        AutoSaveGame();
    }

    private void CancelPendingAutoSave()
    {
        if (pendingAutoSave == null) return;
        StopCoroutine(pendingAutoSave);
        pendingAutoSave = null;
    }

    private SaveSlotInfo ReadSlotInfo(string path, int slotNumber, bool isAutoSave)
    {
        SaveSlotInfo info = new SaveSlotInfo
        {
            slotNumber = slotNumber,
            isAutoSave = isAutoSave,
            exists = SaveFileExists(path)
        };

        if (!info.exists || !TryReadSaveData(path, out SaveData data))
            return info;

        info.isValid = true;
        info.savedAtUtc = data.savedAtUtc;
        info.sceneName = data.sceneName;
        info.moralityBalance = data.moralityBalance;
        info.inventoryItemCount = (data.inventoryItems?.Count ?? 0)
            + (data.equippedItem != null ? 1 : 0);
        return info;
    }

    private static InventoryItemSaveData CreateInventoryItemSaveData(ItemInstance item)
    {
        return new InventoryItemSaveData
        {
            itemId = item.data.itemId,
            x = item.x,
            y = item.y,
            rotated = item.rotated,
            createdByRecipeId = item.createdByRecipeId,
            createdByRecipeRotation = item.createdByRecipeRotation
        };
    }

    private static bool TryReadSaveData(string path, out SaveData data)
    {
        if (TryReadSaveDataFile(path, out data))
            return true;

        string backupPath = GetBackupPath(path);
        if (!File.Exists(backupPath)
            || !TryReadSaveDataFile(backupPath, out data))
        {
            return false;
        }

        Debug.LogWarning($"기본 저장 파일 대신 백업을 복구했습니다: {backupPath}");
        return true;
    }

    private static bool TryReadSaveDataFile(string path, out SaveData data)
    {
        data = null;
        if (!File.Exists(path)) return false;

        try
        {
            string json = File.ReadAllText(path, Utf8);
            data = JsonUtility.FromJson<SaveData>(json);
            if (data == null || string.IsNullOrWhiteSpace(data.sceneName))
                return false;

            if (data.formatVersion > SaveData.CurrentFormatVersion)
            {
                Debug.LogWarning(
                    $"현재 게임보다 새로운 저장 형식입니다: "
                    + $"{data.formatVersion}");
                data = null;
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"저장 파일을 읽지 못했습니다: {path}\n{exception.Message}");
            return false;
        }
    }

    private static void WriteSaveFile(string path, string json)
    {
        string temporaryPath = path + ".tmp";
        string backupPath = GetBackupPath(path);

        try
        {
            File.WriteAllText(temporaryPath, json, Utf8);
            if (!File.Exists(path))
            {
                File.Move(temporaryPath, path);
                return;
            }

            try
            {
                File.Replace(temporaryPath, path, backupPath, true);
            }
            catch (PlatformNotSupportedException)
            {
                ReplaceSaveFileWithFallback(
                    temporaryPath,
                    path,
                    backupPath);
            }
            catch (IOException)
            {
                ReplaceSaveFileWithFallback(
                    temporaryPath,
                    path,
                    backupPath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static void ReplaceSaveFileWithFallback(
        string temporaryPath,
        string path,
        string backupPath)
    {
        File.Copy(path, backupPath, true);
        File.Copy(temporaryPath, path, true);
        File.Delete(temporaryPath);
    }

    private static bool SaveFileExists(string path)
    {
        return File.Exists(path) || File.Exists(GetBackupPath(path));
    }

    private static string GetBackupPath(string path)
    {
        return path + ".bak";
    }

    private string GetManualSavePath(int slotNumber)
    {
        return Path.Combine(SaveDirectory, $"slot_{slotNumber}.json");
    }

    private bool IsAutoSaveExcludedScene(string sceneName)
    {
        if (autoSaveExcludedScenes == null) return false;

        foreach (string excludedScene in autoSaveExcludedScenes)
        {
            if (string.Equals(sceneName, excludedScene, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsValidManualSlot(int slotNumber)
    {
        return slotNumber >= 1 && slotNumber <= ManualSlotCount;
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
