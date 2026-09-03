using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PuzzleModalUI : MonoBehaviour
{
    private enum ModalMode
    {
        None,
        CodeLock,
        Overlay
    }

    private static PuzzleModalUI instance;

    private readonly StringBuilder codeInput = new StringBuilder();

    private GameObject panel;
    private GameObject codeRoot;
    private GameObject overlayRoot;
    private TMP_Text titleText;
    private TMP_Text codeDisplayText;
    private TMP_Text statusText;
    private TMP_Text overlayMessageText;
    private Image overlayBaseImage;
    private Image overlayTopImage;
    private NumericCodeLock activeCodeLock;
    private Action overlayConfirmed;
    private ModalMode mode;
    private int maxCodeLength;

    public static PuzzleModalUI GetOrCreate()
    {
        if (instance != null) return instance;

        PuzzleModalUI existing = FindAnyObjectByType<PuzzleModalUI>();
        if (existing != null) return existing;

        GameObject root = new GameObject(
            nameof(PuzzleModalUI),
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        return root.AddComponent<PuzzleModalUI>();
    }

    public void SetFont(TMP_FontAsset font)
    {
        if (font == null) return;
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            text.font = font;
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

    private void Update()
    {
        if (mode == ModalMode.None) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
            return;
        }

        if (mode != ModalMode.CodeLock) return;

        for (int digit = 0; digit <= 9; digit++)
        {
            KeyCode alphaKey = (KeyCode)((int)KeyCode.Alpha0 + digit);
            KeyCode keypadKey = (KeyCode)((int)KeyCode.Keypad0 + digit);
            if (Input.GetKeyDown(alphaKey)
                || Input.GetKeyDown(keypadKey))
            {
                AppendDigit(digit);
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace)
            || Input.GetKeyDown(KeyCode.Delete))
        {
            Backspace();
        }

        if (Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitCode();
        }
    }

    private void OnDestroy()
    {
        WorldInteractionGate.Unblock(this);
        if (instance == this)
            instance = null;
    }

    public void ShowCodeLock(NumericCodeLock codeLock)
    {
        if (codeLock == null) return;

        activeCodeLock = codeLock;
        overlayConfirmed = null;
        mode = ModalMode.CodeLock;
        maxCodeLength = Mathf.Max(1, codeLock.MaxInputLength);
        codeInput.Clear();

        titleText.text = codeLock.DisplayTitle;
        statusText.text = string.Empty;
        codeRoot.SetActive(true);
        overlayRoot.SetActive(false);
        panel.SetActive(true);
        RefreshCodeDisplay();
        WorldInteractionGate.Block(this);
    }

    public void ShowOverlay(
        string title,
        Sprite baseSprite,
        Sprite topSprite,
        string message,
        Action onConfirmed)
    {
        activeCodeLock = null;
        overlayConfirmed = onConfirmed;
        mode = ModalMode.Overlay;

        titleText.text = string.IsNullOrWhiteSpace(title) ? "겹쳐 보기" : title;
        overlayBaseImage.sprite = baseSprite;
        overlayBaseImage.enabled = baseSprite != null;
        overlayTopImage.sprite = topSprite;
        overlayTopImage.enabled = topSprite != null;
        overlayMessageText.text = message ?? string.Empty;
        statusText.text = string.Empty;
        codeRoot.SetActive(false);
        overlayRoot.SetActive(true);
        panel.SetActive(true);
        WorldInteractionGate.Block(this);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);

        activeCodeLock = null;
        overlayConfirmed = null;
        codeInput.Clear();
        mode = ModalMode.None;
        WorldInteractionGate.Unblock(this);
    }

    private void AppendDigit(int digit)
    {
        if (codeInput.Length >= maxCodeLength) return;
        codeInput.Append(digit);
        statusText.text = string.Empty;
        RefreshCodeDisplay();
    }

    private void Backspace()
    {
        if (codeInput.Length == 0) return;
        codeInput.Length--;
        statusText.text = string.Empty;
        RefreshCodeDisplay();
    }

    private void ClearCode()
    {
        codeInput.Clear();
        statusText.text = string.Empty;
        RefreshCodeDisplay();
    }

    private void SubmitCode()
    {
        if (activeCodeLock == null) return;

        if (activeCodeLock.TrySubmit(codeInput.ToString()))
        {
            Hide();
            return;
        }

        statusText.text = "암호가 맞지 않습니다.";
        codeInput.Clear();
        RefreshCodeDisplay();
    }

    private void ConfirmOverlay()
    {
        Action callback = overlayConfirmed;
        Hide();
        callback?.Invoke();
    }

    private void RefreshCodeDisplay()
    {
        codeDisplayText.text = codeInput.Length == 0
            ? "----"
            : codeInput.ToString();
    }

    private void BuildUi()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5100;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = CreateUiObject("PuzzleModalPanel", transform, typeof(Image));
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0.015f, 0.018f, 0.022f, 0.92f);

        GameObject content = CreateUiObject("Content", panel.transform, typeof(Image));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(560f, 720f);
        content.GetComponent<Image>().color = new Color(0.095f, 0.105f, 0.115f, 1f);

        titleText = CreateText(
            "Title",
            content.transform,
            36f,
            TextAlignmentOptions.Center);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -30f);
        titleRect.sizeDelta = new Vector2(-140f, 60f);

        Button closeButton = CreateButton(
            "Close",
            content.transform,
            "X",
            26f,
            Hide);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-22f, -22f);
        closeRect.sizeDelta = new Vector2(54f, 54f);

        BuildCodeUi(content.transform);
        BuildOverlayUi(content.transform);

        statusText = CreateText(
            "Status",
            content.transform,
            22f,
            TextAlignmentOptions.Center);
        RectTransform statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 20f);
        statusRect.sizeDelta = new Vector2(-60f, 44f);
        statusText.color = new Color(1f, 0.55f, 0.42f, 1f);
    }

    private void BuildCodeUi(Transform parent)
    {
        codeRoot = CreateUiObject("CodeRoot", parent);
        Stretch(codeRoot.GetComponent<RectTransform>());

        codeDisplayText = CreateText(
            "CodeDisplay",
            codeRoot.transform,
            42f,
            TextAlignmentOptions.Center);
        RectTransform displayRect = codeDisplayText.rectTransform;
        displayRect.anchorMin = new Vector2(0.5f, 1f);
        displayRect.anchorMax = new Vector2(0.5f, 1f);
        displayRect.pivot = new Vector2(0.5f, 1f);
        displayRect.anchoredPosition = new Vector2(0f, -112f);
        displayRect.sizeDelta = new Vector2(420f, 72f);

        string[] labels =
        {
            "1", "2", "3",
            "4", "5", "6",
            "7", "8", "9",
            "C", "0", "<"
        };

        const float buttonSize = 88f;
        const float gap = 18f;
        const float startY = -224f;
        for (int i = 0; i < labels.Length; i++)
        {
            string label = labels[i];
            Button button = CreateButton(
                $"Key{label}",
                codeRoot.transform,
                label,
                30f,
                () => HandleCodeButton(label));
            RectTransform rect = button.GetComponent<RectTransform>();
            int column = i % 3;
            int row = i / 3;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(
                (column - 1) * (buttonSize + gap),
                startY - row * (buttonSize + gap));
            rect.sizeDelta = new Vector2(buttonSize, buttonSize);
        }

        Button submitButton = CreateButton(
            "Submit",
            codeRoot.transform,
            "확인",
            27f,
            SubmitCode);
        RectTransform submitRect = submitButton.GetComponent<RectTransform>();
        submitRect.anchorMin = new Vector2(0.5f, 1f);
        submitRect.anchorMax = new Vector2(0.5f, 1f);
        submitRect.pivot = new Vector2(0.5f, 1f);
        submitRect.anchoredPosition = new Vector2(0f, -650f);
        submitRect.sizeDelta = new Vector2(300f, 58f);
    }

    private void BuildOverlayUi(Transform parent)
    {
        overlayRoot = CreateUiObject("OverlayRoot", parent);
        Stretch(overlayRoot.GetComponent<RectTransform>());

        GameObject imageArea = CreateUiObject(
            "ImageArea",
            overlayRoot.transform,
            typeof(Image));
        RectTransform imageAreaRect = imageArea.GetComponent<RectTransform>();
        imageAreaRect.anchorMin = new Vector2(0.5f, 1f);
        imageAreaRect.anchorMax = new Vector2(0.5f, 1f);
        imageAreaRect.pivot = new Vector2(0.5f, 1f);
        imageAreaRect.anchoredPosition = new Vector2(0f, -112f);
        imageAreaRect.sizeDelta = new Vector2(410f, 360f);
        imageArea.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.05f, 1f);

        overlayBaseImage = CreateOverlayImage("BaseImage", imageArea.transform);
        overlayTopImage = CreateOverlayImage("TopImage", imageArea.transform);
        Color topColor = overlayTopImage.color;
        topColor.a = 0.72f;
        overlayTopImage.color = topColor;

        overlayMessageText = CreateText(
            "Message",
            overlayRoot.transform,
            25f,
            TextAlignmentOptions.Center);
        overlayMessageText.textWrappingMode = TextWrappingModes.Normal;
        RectTransform messageRect = overlayMessageText.rectTransform;
        messageRect.anchorMin = new Vector2(0.5f, 1f);
        messageRect.anchorMax = new Vector2(0.5f, 1f);
        messageRect.pivot = new Vector2(0.5f, 1f);
        messageRect.anchoredPosition = new Vector2(0f, -500f);
        messageRect.sizeDelta = new Vector2(450f, 90f);

        Button confirmButton = CreateButton(
            "ConfirmOverlay",
            overlayRoot.transform,
            "확인",
            27f,
            ConfirmOverlay);
        RectTransform confirmRect = confirmButton.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 1f);
        confirmRect.anchorMax = new Vector2(0.5f, 1f);
        confirmRect.pivot = new Vector2(0.5f, 1f);
        confirmRect.anchoredPosition = new Vector2(0f, -620f);
        confirmRect.sizeDelta = new Vector2(300f, 58f);
    }

    private void HandleCodeButton(string label)
    {
        if (label == "C")
        {
            ClearCode();
            return;
        }

        if (label == "<")
        {
            Backspace();
            return;
        }

        if (int.TryParse(label, out int digit))
            AppendDigit(digit);
    }

    private static Image CreateOverlayImage(string name, Transform parent)
    {
        GameObject imageObject = CreateUiObject(name, parent, typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        Stretch(rect);
        rect.offsetMin = new Vector2(18f, 18f);
        rect.offsetMax = new Vector2(-18f, -18f);

        Image image = imageObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        float fontSize,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUiObject(
            name,
            parent,
            typeof(Image),
            typeof(Button));
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.19f, 0.23f, 0.25f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.82f, 0.9f, 0.86f, 1f);
        colors.pressedColor = new Color(0.6f, 0.72f, 0.66f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        TMP_Text text = CreateText(
            "Label",
            buttonObject.transform,
            fontSize,
            TextAlignmentOptions.Center);
        text.text = label;
        Stretch(text.rectTransform);
        return button;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(
            name,
            parent,
            typeof(TextMeshProUGUI));
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.95f, 0.93f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static GameObject CreateUiObject(
        string name,
        Transform parent,
        params Type[] components)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        foreach (Type component in components)
            gameObject.AddComponent(component);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
