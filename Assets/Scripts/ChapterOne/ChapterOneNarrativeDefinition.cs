using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Narrative/Chapter One Definition")]
public sealed class ChapterOneNarrativeDefinition : ScriptableObject
{
    [Serializable]
    public sealed class ProgressTextRule
    {
        [SerializeField]
        private OnlyOneUnityString[] required = Array.Empty<OnlyOneUnityString>();

        [SerializeField]
        private OnlyOneUnityString[] excluded = Array.Empty<OnlyOneUnityString>();

        [SerializeField, TextArea(2, 5)]
        private string text;

        public bool Matches()
        {
            foreach (OnlyOneUnityString id in required)
            {
                if (!GameProgressState.IsPuzzleCompleted(id))
                    return false;
            }

            foreach (OnlyOneUnityString id in excluded)
            {
                if (GameProgressState.IsPuzzleCompleted(id))
                    return false;
            }

            return true;
        }

        public string Text => text;
    }

    [Header("Opening")]
    [SerializeField]
    private OnlyOneUnityString introductionId;

    [SerializeField]
    private DialogueSequence openingDialogue;

    [Header("HUD")]
    [SerializeField]
    private List<ProgressTextRule> objectiveRules = new();

    [SerializeField]
    private List<ProgressTextRule> parentHintRules = new();

    [Header("Chapter Events")]
    [SerializeField, TextArea(2, 5)]
    private string clueHint;

    [SerializeField, TextArea(2, 5)]
    private string ballTrackStartedHint;

    [SerializeField]
    private DialogueSequence ballTrackCompletedDialogue;

    public bool ShouldPlayOpening => !GameProgressState.IsPuzzleCompleted(introductionId);
    public IReadOnlyList<string> OpeningLines => openingDialogue != null
        ? openingDialogue.Lines
        : Array.Empty<string>();
    public string ObjectiveText => ResolveFirstMatching(objectiveRules);
    public string ParentHintText => ResolveFirstMatching(parentHintRules);
    public string ClueHint => clueHint;
    public string BallTrackStartedHint => ballTrackStartedHint;
    public IReadOnlyList<string> BallTrackCompletedLines => ballTrackCompletedDialogue != null
        ? ballTrackCompletedDialogue.Lines
        : Array.Empty<string>();

    public void MarkOpeningPresented()
    {
        GameProgressState.CompletePuzzle(introductionId);
    }

    private static string ResolveFirstMatching(IEnumerable<ProgressTextRule> rules)
    {
        foreach (ProgressTextRule rule in rules)
        {
            if (rule != null && rule.Matches())
                return rule.Text;
        }

        return string.Empty;
    }
}
