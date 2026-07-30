using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SceneTransitionInteractable : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private string chapterTitle;
    [SerializeField] private List<string> requiredPuzzleIds =
        new List<string>();
    [SerializeField] private bool saveBeforeTransition = true;
    [SerializeField] private UnityEvent onTransitionStarted;
    [SerializeField] private UnityEvent onTransitionBlocked;

    public bool CanTransition
    {
        get
        {
            foreach (string puzzleId in requiredPuzzleIds)
            {
                if (!GameProgressState.IsPuzzleCompleted(puzzleId))
                    return false;
            }

            return true;
        }
    }

    public void Interact()
    {
        if (!CanTransition)
        {
            onTransitionBlocked?.Invoke();
            return;
        }

        if (saveBeforeTransition && SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.AutoSaveGame();

        if (SceneTransitionService.GetOrCreate().LoadScene(
                targetSceneName,
                chapterTitle))
        {
            onTransitionStarted?.Invoke();
        }
        else
        {
            onTransitionBlocked?.Invoke();
        }
    }
}
