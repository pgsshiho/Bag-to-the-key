using UnityEngine;

public class MoralityEndingDialogue : MonoBehaviour
{
    [SerializeField] private DialogueTextController dialogueController;
    [SerializeField, Min(1)] private int alignmentThreshold = 1;

    [Header("Ending Lines")]
    [SerializeField, TextArea(2, 5)] private string virtueEndingLine;
    [SerializeField, TextArea(2, 5)] private string neutralEndingLine;
    [SerializeField, TextArea(2, 5)] private string sinEndingLine;

    public string GetEndingLine()
    {
        int balance = GameProgressState.MoralityBalance;
        if (balance >= alignmentThreshold)
            return virtueEndingLine;
        if (balance <= -alignmentThreshold)
            return sinEndingLine;
        return neutralEndingLine;
    }

    public void ShowEndingLine()
    {
        if (dialogueController == null)
        {
            Debug.LogWarning(
                $"{name}: Dialogue Text Controller is not assigned.",
                this);
            return;
        }

        string line = GetEndingLine();
        if (!string.IsNullOrWhiteSpace(line))
            dialogueController.PlayDialogue(line);
    }
}
