using UnityEngine;
using UnityEngine.Events;

public class PuzzleOutcomeRecorder : MonoBehaviour
{
    [Header("Progress IDs")]
    [SerializeField] private string puzzleId;
    [SerializeField] private string outcomeId;

    [Header("Morality")]
    [Tooltip("Positive values are virtue. Negative values are sin.")]
    [SerializeField] private int moralityDelta;

    [Header("Result")]
    [SerializeField] private UnityEvent onResolvedStateApplied;
    [SerializeField] private UnityEvent onFirstResolved;

    private bool resolvedStateApplied;

    public string PuzzleId => puzzleId;
    public string OutcomeId => outcomeId;
    public int MoralityDelta => moralityDelta;
    public bool IsResolved => GameProgressState.HasOutcome(outcomeId);

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += RefreshResolvedState;
    }

    private void Start()
    {
        RefreshResolvedState();
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= RefreshResolvedState;
    }

    public bool Resolve()
    {
        if (string.IsNullOrWhiteSpace(puzzleId)
            || string.IsNullOrWhiteSpace(outcomeId))
        {
            Debug.LogWarning(
                $"{name}: Puzzle ID and Outcome ID must be assigned.",
                this);
            return false;
        }

        bool firstResolution = GameProgressState.RecordOutcome(
            puzzleId,
            outcomeId,
            moralityDelta);

        RefreshResolvedState();
        if (firstResolution)
            onFirstResolved?.Invoke();
        return IsResolved;
    }

    private void RefreshResolvedState()
    {
        if (!IsResolved)
        {
            resolvedStateApplied = false;
            return;
        }

        if (resolvedStateApplied) return;
        resolvedStateApplied = true;
        onResolvedStateApplied?.Invoke();
    }
}
