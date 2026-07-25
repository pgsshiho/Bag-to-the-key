using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameProgressState
{
    private static readonly HashSet<string> completedPuzzleIds = new();
    private static readonly HashSet<string> recordedOutcomeIds = new();

    public static event Action<int> MoralityChanged;
    public static event Action ProgressChanged;

    public static int MoralityBalance { get; private set; }
    public static IReadOnlyCollection<string> CompletedPuzzleIds => completedPuzzleIds;
    public static IReadOnlyCollection<string> RecordedOutcomeIds => recordedOutcomeIds;

    public static bool IsPuzzleCompleted(string puzzleId)
    {
        return !string.IsNullOrWhiteSpace(puzzleId)
            && completedPuzzleIds.Contains(puzzleId);
    }

    public static bool HasOutcome(string outcomeId)
    {
        return !string.IsNullOrWhiteSpace(outcomeId)
            && recordedOutcomeIds.Contains(outcomeId);
    }

    public static bool CompletePuzzle(string puzzleId)
    {
        if (string.IsNullOrWhiteSpace(puzzleId)) return false;
        if (!completedPuzzleIds.Add(puzzleId)) return false;

        ProgressChanged?.Invoke();
        return true;
    }

    public static bool RecordOutcome(
        string puzzleId,
        string outcomeId,
        int moralityDelta)
    {
        if (string.IsNullOrWhiteSpace(puzzleId)
            || string.IsNullOrWhiteSpace(outcomeId))
            return false;

        if (completedPuzzleIds.Contains(puzzleId)
            || recordedOutcomeIds.Contains(outcomeId))
            return false;

        completedPuzzleIds.Add(puzzleId);
        recordedOutcomeIds.Add(outcomeId);
        MoralityBalance += moralityDelta;
        MoralityChanged?.Invoke(MoralityBalance);
        ProgressChanged?.Invoke();
        return true;
    }

    public static void Restore(
        int moralityBalance,
        IEnumerable<string> puzzleIds,
        IEnumerable<string> outcomeIds)
    {
        completedPuzzleIds.Clear();
        recordedOutcomeIds.Clear();
        AddValidIds(completedPuzzleIds, puzzleIds);
        AddValidIds(recordedOutcomeIds, outcomeIds);
        MoralityBalance = moralityBalance;
        MoralityChanged?.Invoke(MoralityBalance);
        ProgressChanged?.Invoke();
    }

    public static void Reset()
    {
        completedPuzzleIds.Clear();
        recordedOutcomeIds.Clear();
        MoralityBalance = 0;
        MoralityChanged?.Invoke(MoralityBalance);
        ProgressChanged?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        completedPuzzleIds.Clear();
        recordedOutcomeIds.Clear();
        MoralityBalance = 0;
        MoralityChanged = null;
        ProgressChanged = null;
    }

    private static void AddValidIds(
        HashSet<string> target,
        IEnumerable<string> source)
    {
        if (source == null) return;

        foreach (string id in source)
        {
            if (!string.IsNullOrWhiteSpace(id))
                target.Add(id);
        }
    }
}
