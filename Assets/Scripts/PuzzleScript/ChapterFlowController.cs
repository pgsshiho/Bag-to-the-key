using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ChapterFlowController : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private string chapterCompletionId;
    [SerializeField, TextArea] private string objectiveText;
    [SerializeField] private TMP_Text objectiveLabel;
    [SerializeField] private List<string> requiredPuzzleIds =
        new List<string>();
    [SerializeField] private string nextSceneName;
    [SerializeField] private string nextChapterTitle;
    [SerializeField] private bool saveBeforeTransition = true;
    [SerializeField] private UnityEvent onExitReady;
    [SerializeField] private UnityEvent onExitBlocked;
    [SerializeField] private UnityEvent onChapterCompleted;

    private bool readyStateApplied;

    public bool IsExitReady
    {
        get
        {
            if (requiredPuzzleIds.Count == 0)
                return false;

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
        GameProgressState.ProgressChanged += Refresh;
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= Refresh;
    }

    public void Interact()
    {
        if (!IsExitReady)
        {
            onExitBlocked?.Invoke();
            return;
        }

        if (!string.IsNullOrWhiteSpace(chapterCompletionId))
            GameProgressState.CompletePuzzle(chapterCompletionId);
        onChapterCompleted?.Invoke();

        if (saveBeforeTransition && SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.AutoSaveGame();

        SceneTransitionService.GetOrCreate().LoadScene(
            nextSceneName,
            nextChapterTitle);
    }

    public void Refresh()
    {
        if (objectiveLabel != null)
            objectiveLabel.text = objectiveText;

        if (!IsExitReady)
        {
            readyStateApplied = false;
            return;
        }

        if (readyStateApplied) return;
        readyStateApplied = true;
        onExitReady?.Invoke();
    }
}
