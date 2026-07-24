using System.Collections.Generic;
using UnityEngine;

public class DialogueInteractable : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private DialogueTextController dialogueController;
    [SerializeField, TextArea(2, 5)] private List<string> dialogueLines = new();

    private int nextLineIndex;

    public void Interact()
    {
        if (dialogueController == null)
        {
            Debug.LogWarning(
                $"{name}: Dialogue Text Controller is not assigned.",
                this);
            return;
        }

        if (dialogueLines.Count == 0)
        {
            Debug.LogWarning(
                $"{name}: Dialogue Lines are empty.",
                this);
            return;
        }

        ShowNextLine(continueAtCurrentPosition: false);
    }

    private void ShowNextLine(bool continueAtCurrentPosition)
    {
        nextLineIndex = Mathf.Clamp(nextLineIndex, 0, dialogueLines.Count - 1);
        int lineIndex = nextLineIndex;
        System.Action continuation =
            () => ShowNextLine(continueAtCurrentPosition: true);

        if (continueAtCurrentPosition)
        {
            dialogueController.ContinueDialogue(
                dialogueLines[lineIndex],
                continuation);
        }
        else
        {
            dialogueController.PlayDialogue(
                dialogueLines[lineIndex],
                continuation);
        }

        nextLineIndex = (lineIndex + 1) % dialogueLines.Count;
    }
}
