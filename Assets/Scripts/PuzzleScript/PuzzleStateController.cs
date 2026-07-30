using UnityEngine;
using UnityEngine.Events;

public class PuzzleStateController : MonoBehaviour
{
    [SerializeField] private string puzzleId;
    [SerializeField] private UnityEvent onCompletedStateApplied;
    [SerializeField] private UnityEvent onFirstCompleted;

    private bool completedStateApplied;

    public string PuzzleId => puzzleId;
    public bool IsCompleted => GameProgressState.IsPuzzleCompleted(puzzleId);

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += RefreshState;
        RefreshState();
    }

    private void Start()
    {
        RefreshState();
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= RefreshState;
    }

    public bool Complete()
    {
        if (string.IsNullOrWhiteSpace(puzzleId))
        {
            Debug.LogWarning($"{name}: Puzzle ID가 지정되지 않았습니다.", this);
            return false;
        }

        bool firstCompletion = GameProgressState.CompletePuzzle(puzzleId);
        RefreshState();
        if (firstCompletion)
            onFirstCompleted?.Invoke();
        return IsCompleted;
    }

    public void RefreshState()
    {
        if (!IsCompleted)
        {
            completedStateApplied = false;
            return;
        }

        if (completedStateApplied) return;
        completedStateApplied = true;
        onCompletedStateApplied?.Invoke();
    }
}
