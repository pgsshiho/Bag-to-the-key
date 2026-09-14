using UnityEngine;

public class ChapterOneClue : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private InvestigationCameraController cameraController;
    [SerializeField] private InvestigationPoint investigationPoint;
    [SerializeField] private ChapterOnePresentation presentation;
    [SerializeField] private OnlyOneUnityString clueDiscoveredId;

    public void Interact()
    {
        if (cameraController == null || !cameraController.TryFocus(investigationPoint)) return;
        GameProgressState.CompletePuzzle(clueDiscoveredId);
        presentation.ShowClueHint();
    }
}
