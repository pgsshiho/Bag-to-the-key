using UnityEngine;

[CreateAssetMenu(menuName = "UI/Text Catalog")]
public sealed class UiTextCatalog : ScriptableObject
{
    private const string ResourcePath = "UI/UiTextCatalog";

    private static UiTextCatalog cachedInstance;

    [Header("Puzzle Modal")]
    [SerializeField, TextArea]
    private string defaultOverlayTitle;

    [SerializeField, TextArea]
    private string defaultCodeLockTitle;

    [SerializeField, TextArea]
    private string invalidCodeMessage;

    [SerializeField]
    private string emptyCodePlaceholder;

    [Header("Save Slots")]
    [SerializeField]
    private string saveGameTitle;

    [SerializeField]
    private string loadGameTitle;

    [SerializeField]
    private string manualSlotFormat;

    [SerializeField]
    private string autoSaveLabel;

    [SerializeField]
    private string emptySlotLabel;

    [SerializeField]
    private string corruptedSlotLabel;

    public string DefaultOverlayTitle => defaultOverlayTitle;
    public string DefaultCodeLockTitle => defaultCodeLockTitle;
    public string InvalidCodeMessage => invalidCodeMessage;
    public string EmptyCodePlaceholder => emptyCodePlaceholder;
    public string SaveGameTitle => saveGameTitle;
    public string LoadGameTitle => loadGameTitle;
    public string ManualSlotFormat => manualSlotFormat;
    public string AutoSaveLabel => autoSaveLabel;
    public string EmptySlotLabel => emptySlotLabel;
    public string CorruptedSlotLabel => corruptedSlotLabel;

    public static UiTextCatalog Load()
    {
        if (cachedInstance != null)
            return cachedInstance;

        cachedInstance = Resources.Load<UiTextCatalog>(ResourcePath);
        if (cachedInstance == null)
        {
            throw new System.InvalidOperationException(
                $"Missing authored UI text catalog: Resources/{ResourcePath}"
            );
        }

        return cachedInstance;
    }
}
