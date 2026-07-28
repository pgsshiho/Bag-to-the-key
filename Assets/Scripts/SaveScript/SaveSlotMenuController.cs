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

    private readonly List<Button> manualSlotButtons = new List<Button>();
    private readonly List<TMP_Text> manualSlotLabels = new List<TMP_Text>();

    private SaveLoadManager saveLoadManager;
    private SaveSlotMenuMode mode;
    private Canvas canvas;
    private GameObject panel;
    private TMP_Text title;
    private Button autoSaveButton;
    private TMP_Text autoSaveLabel;

    public static SaveSlotMenuController GetOrCreate()
    {
        if (instance != null) return instance;

        SaveSlotMenuController existing = FindAnyObjectByType<SaveSlotMenuController>();
        if (existing != null) return existing;

        GameObject root = new GameObject(
            nameof(SaveSlotMenuController),
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        return root.AddComponent<SaveSlotMenuController>();
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
        BuildUi();
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

        title.text = mode == SaveSlotMenuMode.Save ? "SAVE GAME" : "LOAD GAME";
        SaveSlotInfo[] slots = saveLoadManager.GetSaveSlots();

        for (int i = 0; i < SaveLoadManager.ManualSlotCount; i++)
        {
            int slotNumber = i + 1;
            SaveSlotInfo info = slots[i];
            Button button = manualSlotButtons[i];
            TMP_Text label = manualSlotLabels[i];

            label.text = FormatSlotLabel($"SLOT {slotNumber}", info);
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
        autoSaveLabel.text = FormatSlotLabel("AUTOSAVE", autoSaveInfo);
        autoSaveButton.interactable = autoSaveInfo.isValid;
        autoSaveButton.onClick.RemoveAllListeners();
        autoSaveButton.onClick.AddListener(() =>
        {
            saveLoadManager.LoadAutoSave();
            Hide();
        });
    }

    private void BuildUi()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = CreateUiObject("SaveSlotPanel", transform, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        Stretch(panelRect);
        panel.GetComponent<Image>().color = new Color(0.025f, 0.025f, 0.03f, 0.94f);

        GameObject content = CreateUiObject("Content", panel.transform, typeof(Image));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(760f, 720f);
        contentRect.anchoredPosition = Vector2.zero;
        content.GetComponent<Image>().color = new Color(0.09f, 0.09f, 0.105f, 1f);

        title = CreateText("Title", content.transform, 38, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -34f);
        titleRect.sizeDelta = new Vector2(-160f, 64f);

        Button closeButton = CreateButton("Close", content.transform, "X", out _);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-24f, -24f);
        closeRect.sizeDelta = new Vector2(56f, 56f);
        closeButton.onClick.AddListener(Hide);

        const float firstRowY = -124f;
        const float rowSpacing = 88f;
        for (int i = 0; i < SaveLoadManager.ManualSlotCount; i++)
        {
            Button button = CreateButton(
                $"ManualSlot{i + 1}",
                content.transform,
                string.Empty,
                out TMP_Text label);
            PositionRow(button.GetComponent<RectTransform>(), firstRowY - i * rowSpacing);
            manualSlotButtons.Add(button);
            manualSlotLabels.Add(label);
        }

        autoSaveButton = CreateButton(
            "AutoSaveSlot",
            content.transform,
            string.Empty,
            out autoSaveLabel);
        PositionRow(
            autoSaveButton.GetComponent<RectTransform>(),
            firstRowY - SaveLoadManager.ManualSlotCount * rowSpacing);
    }

    private static Button CreateButton(
        string objectName,
        Transform parent,
        string labelText,
        out TMP_Text label)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent, typeof(Image), typeof(Button));
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.17f, 0.17f, 0.2f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.86f, 0.86f, 0.9f, 1f);
        colors.pressedColor = new Color(0.68f, 0.68f, 0.74f, 1f);
        colors.disabledColor = new Color(0.42f, 0.42f, 0.45f, 0.55f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        label = CreateText("Label", buttonObject.transform, 25, TextAlignmentOptions.MidlineLeft);
        label.text = labelText;
        label.margin = new Vector4(28f, 0f, 28f, 0f);
        Stretch(label.rectTransform);
        return button;
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(objectName, parent, typeof(TextMeshProUGUI));
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.94f, 0.96f, 1f);
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static GameObject CreateUiObject(
        string objectName,
        Transform parent,
        params Type[] components)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        foreach (Type component in components)
            gameObject.AddComponent(component);

        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void PositionRow(RectTransform rect, float y)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(660f, 68f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static string FormatSlotLabel(string prefix, SaveSlotInfo info)
    {
        if (!info.exists) return $"{prefix}    EMPTY";
        if (!info.isValid) return $"{prefix}    CORRUPTED";

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
