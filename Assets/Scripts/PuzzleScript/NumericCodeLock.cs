using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PuzzleStateController))]
public class NumericCodeLock : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private string displayTitle = "암호 입력";
    [SerializeField] private string expectedCode = "0000";
    [SerializeField, Min(1)] private int maxInputLength = 4;
    [SerializeField] private UnityEvent onCorrectCode;
    [SerializeField] private UnityEvent onWrongCode;
    [SerializeField] private UnityEvent onAlreadyUnlocked;

    private PuzzleStateController completion;

    public string DisplayTitle => displayTitle;
    public int MaxInputLength => Mathf.Max(maxInputLength, expectedCode?.Length ?? 0);

    private void Awake()
    {
        completion = GetComponent<PuzzleStateController>();
    }

    private void OnValidate()
    {
        maxInputLength = Mathf.Max(1, maxInputLength);
    }

    public void Interact()
    {
        if (completion == null)
            completion = GetComponent<PuzzleStateController>();

        if (completion.IsCompleted)
        {
            onAlreadyUnlocked?.Invoke();
            return;
        }

        PuzzleModalUI.GetOrCreate().ShowCodeLock(this);
    }

    public bool TrySubmit(string enteredCode)
    {
        if (completion == null)
            completion = GetComponent<PuzzleStateController>();

        if (completion.IsCompleted)
            return true;

        if (!string.Equals(enteredCode, expectedCode, System.StringComparison.Ordinal))
        {
            onWrongCode?.Invoke();
            return false;
        }

        completion.Complete();
        onCorrectCode?.Invoke();
        return true;
    }
}
