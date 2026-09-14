using System.Collections;
using TMPro;
using UnityEngine;

public class ChapterOnePresentation : MonoBehaviour
{
    [SerializeField]
    private DialogueTextController dialogue;

    [SerializeField]
    private TMP_Text objective;

    [SerializeField]
    private TMP_Text hint;

    [SerializeField]
    private CanvasGroup opening;

    [SerializeField]
    private ChapterOneNarrativeDefinition narrative;

    [SerializeField, Min(0f)]
    private float openingDelay = 1.2f;

    [SerializeField, Min(0f)]
    private float openingFadeDuration = 1.2f;

    [SerializeField]
    private ChapterFlowController exit;

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= Refresh;
        WorldInteractionGate.Unblock(this);
    }

    private IEnumerator Start()
    {
        // Let saved inventory/progress and the scene transition restore first.
        yield return null;
        while (
            SceneTransitionService.Instance != null
            && SceneTransitionService.Instance.IsTransitioning
        )
            yield return null;

        WorldInteractionGate.Block(this);
        yield return CanvasGroupFadeSequence.FadeOut(
            opening,
            openingDelay,
            openingFadeDuration
        );
        WorldInteractionGate.Unblock(this);
        Refresh();
        if (narrative != null && narrative.ShouldPlayOpening)
        {
            narrative.MarkOpeningPresented();
            dialogue.PlayDialogue(narrative.OpeningLines);
        }
    }

    public void Say(string message)
    {
        SetHint(message);
        if (dialogue != null)
            dialogue.PlayDialogue(new[] { message });
    }

    public void SetHint(string message)
    {
        if (hint != null)
            hint.text = message;
    }

    public void Refresh()
    {
        if (objective == null)
            return;
        objective.text = narrative != null ? narrative.ObjectiveText : string.Empty;
    }

    public void ParentHint()
    {
        if (narrative != null)
            Say(narrative.ParentHintText);
    }

    public void ShowClueHint()
    {
        if (narrative != null)
            SetHint(narrative.ClueHint);
    }

    public void ShowBallTrackStartedHint()
    {
        if (narrative != null)
            SetHint(narrative.BallTrackStartedHint);
    }

    public void ShowBallTrackCompletedDialogue()
    {
        if (narrative == null)
            return;

        System.Collections.Generic.IReadOnlyList<string> lines = narrative.BallTrackCompletedLines;
        if (lines.Count == 0)
            return;

        SetHint(lines[0]);
        if (dialogue != null)
            dialogue.PlayDialogue(lines);
    }

    public void LeaveRoom()
    {
        if (!WorldInteractionGate.IsBlocked)
            exit.Interact();
    }
}
