using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum SaveSlotMenuMode
{
    Save,
    Load
}

public class SaveSlotMenuController : MonoBehaviour
{
    private static SaveSlotMenuController instance;

    [SerializeField] private List<Button> manualSlotButtons = new List<Button>();
    [SerializeField] private List<TMP_Text> manualSlotLabels = new List<TMP_Text>();

    [SerializeField] private Button closeButton;

    private SaveLoadManager saveLoadManager;
    private SaveSlotMenuMode mode;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text title;
    [SerializeField] private Button autoSaveButton;
    [SerializeField] private TMP_Text autoSaveLabel;

    public static SaveSlotMenuController GetOrCreate()
    {
        if (instance != null) return instance;

        SaveSlotMenuController existing = FindAnyObjectByType<SaveSlotMenuController>();
        if (existing != null) return existing;

        SaveSlotMenuController prefab = Resources.Load<SaveSlotMenuController>("UI/SaveSlotMenuController");
        if (prefab == null)
            throw new InvalidOperationException("Missing authored UI prefab: Resources/UI/SaveSlotMenuController");
        return Instantiate(prefab);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        closeButton.onClick.AddListener(Hide);
        EnsureEventSystem();
        Hide();
    }

    private void OnDestroy()
    {
        if (saveLoadManager != null)
            saveLoadManager.SaveSlotsChanged -= Refresh;
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    public void Show(SaveLoadManager manager, SaveSlotMenuMode menuMode)
    {
        if (saveLoadManager != manager)
        {
            if (saveLoadManager != null)
                saveLoadManager.SaveSlotsChanged -= Refresh;

            saveLoadManager = manager;
            if (saveLoadManager != null)
                saveLoadManager.SaveSlotsChanged += Refresh;
        }

        mode = menuMode;
        panel.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void Refresh()
    {
        if (saveLoadManager == null || panel == null) return;

        UiTextCatalog textCatalog = UiTextCatalog.Load();
        title.text = mode == SaveSlotMenuMode.Save
            ? textCatalog.SaveGameTitle
            : textCatalog.LoadGameTitle;
        SaveSlotInfo[] slots = saveLoadManager.GetSaveSlots();

        for (int i = 0; i < SaveLoadManager.ManualSlotCount; i++)
        {
            int slotNumber = i + 1;
            SaveSlotInfo info = slots[i];
            Button button = manualSlotButtons[i];
            TMP_Text label = manualSlotLabels[i];

            label.text = FormatSlotLabel(
                string.Format(textCatalog.ManualSlotFormat, slotNumber),
                info,
                textCatalog
            );
            button.onClick.RemoveAllListeners();

            if (mode == SaveSlotMenuMode.Save)
            {
                button.interactable = true;
                button.onClick.AddListener(() =>
                {
                    saveLoadManager.SaveGame(slotNumber);
                    Hide();
                });
            }
            else
            {
                button.interactable = info.isValid;
                button.onClick.AddListener(() =>
                {
                    saveLoadManager.LoadGame(slotNumber);
                    Hide();
                });
            }
        }

        SaveSlotInfo autoSaveInfo = slots[SaveLoadManager.ManualSlotCount];
        autoSaveButton.gameObject.SetActive(mode == SaveSlotMenuMode.Load);
        autoSaveLabel.text = FormatSlotLabel(textCatalog.AutoSaveLabel, autoSaveInfo, textCatalog);
        autoSaveButton.interactable = autoSaveInfo.isValid;
        autoSaveButton.onClick.RemoveAllListeners();
        autoSaveButton.onClick.AddListener(() =>
        {
            saveLoadManager.LoadAutoSave();
            Hide();
        });
    }

    private static string FormatSlotLabel(
        string prefix,
        SaveSlotInfo info,
        UiTextCatalog textCatalog
    )
    {
        if (!info.exists) return $"{prefix}    {textCatalog.EmptySlotLabel}";
        if (!info.isValid) return $"{prefix}    {textCatalog.CorruptedSlotLabel}";

        string savedAt = string.Empty;
        if (DateTime.TryParse(
                info.savedAtUtc,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out DateTime utcTime))
        {
            savedAt = utcTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        string separator = string.IsNullOrEmpty(savedAt) ? string.Empty : $"    {savedAt}";
        return $"{prefix}    {info.sceneName}{separator}";
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));
    }
}
