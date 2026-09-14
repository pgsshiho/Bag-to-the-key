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
        Overlay,
    }

    private static PuzzleModalUI instance;

    private readonly StringBuilder codeInput = new StringBuilder();

    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private GameObject codeRoot;

    [SerializeField]
    private GameObject overlayRoot;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text codeDisplayText;

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private TMP_Text overlayMessageText;

    [SerializeField]
    private Image overlayBaseImage;

    [SerializeField]
    private Image overlayTopImage;

    [SerializeField]
    private Button closeButton;

    [SerializeField]
    private Button submitButton;

    [SerializeField]
    private Button confirmButton;

    [SerializeField]
    private Button clearButton;

    [SerializeField]
    private Button backspaceButton;

    [SerializeField]
    private Button[] digitButtons;

    private NumericCodeLock activeCodeLock;
    private Action overlayConfirmed;
    private ModalMode mode;
    private int maxCodeLength;

    public static PuzzleModalUI GetOrCreate()
    {
        if (instance != null)
            return instance;

        PuzzleModalUI existing = FindAnyObjectByType<PuzzleModalUI>();
        if (existing != null)
            return existing;

        PuzzleModalUI prefab = Resources.Load<PuzzleModalUI>("UI/PuzzleModalUI");
        if (prefab == null)
            throw new InvalidOperationException(
                "Missing authored UI prefab: Resources/UI/PuzzleModalUI"
            );
        return Instantiate(prefab);
    }

    public void SetFont(TMP_FontAsset font)
    {
        if (font == null)
            return;
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
        closeButton.onClick.AddListener(Hide);
        submitButton.onClick.AddListener(SubmitCode);
        confirmButton.onClick.AddListener(ConfirmOverlay);
        clearButton.onClick.AddListener(ClearCode);
        backspaceButton.onClick.AddListener(Backspace);
        for (int i = 0; i < digitButtons.Length; i++)
        {
            int digit = i;
            digitButtons[i].onClick.AddListener(() => AppendDigit(digit));
        }
        EnsureEventSystem();
        Hide();
    }

    private void Update()
    {
        if (mode == ModalMode.None)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
            return;
        }

        if (mode != ModalMode.CodeLock)
            return;

        for (int digit = 0; digit <= 9; digit++)
        {
            KeyCode alphaKey = (KeyCode)((int)KeyCode.Alpha0 + digit);
            KeyCode keypadKey = (KeyCode)((int)KeyCode.Keypad0 + digit);
            if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
            {
                AppendDigit(digit);
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            Backspace();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
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
        if (codeLock == null)
            return;

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
        Action onConfirmed
    )
    {
        activeCodeLock = null;
        overlayConfirmed = onConfirmed;
        mode = ModalMode.Overlay;

        titleText.text = string.IsNullOrWhiteSpace(title) ? UiTextCatalog.Load().DefaultOverlayTitle : title;
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
        if (codeInput.Length >= maxCodeLength)
            return;
        codeInput.Append(digit);
        statusText.text = string.Empty;
        RefreshCodeDisplay();
    }

    private void Backspace()
    {
        if (codeInput.Length == 0)
            return;
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
        if (activeCodeLock == null)
            return;

        if (activeCodeLock.TrySubmit(codeInput.ToString()))
        {
            Hide();
            return;
        }

        statusText.text = UiTextCatalog.Load().InvalidCodeMessage;
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
            ? UiTextCatalog.Load().EmptyCodePlaceholder
            : codeInput.ToString();
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
