using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleCompletionCondition : MonoBehaviour
{
    [SerializeField] private List<string> requiredPuzzleIds = new();
    [SerializeField] private UnityEvent onConditionSatisfied;

    private bool conditionApplied;

    public bool IsSatisfied
    {
        get
        {
            if (requiredPuzzleIds.Count == 0) return false;

            foreach (string puzzleId in requiredPuzzleIds)
            {
                if (!GameProgressState.IsPuzzleCompleted(puzzleId))
                    return false;
            }

            return true;
        }
    }

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += RefreshCondition;
    }

    private void Start()
    {
        RefreshCondition();
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= RefreshCondition;
    }

    public void RefreshCondition()
    {
        if (!IsSatisfied)
        {
            conditionApplied = false;
            return;
        }

        if (conditionApplied) return;
        conditionApplied = true;
        onConditionSatisfied?.Invoke();
    }
}
