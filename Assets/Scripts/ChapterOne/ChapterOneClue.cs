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
        presentation.SetHint("구멍 너머의 숫자를 왼쪽부터 기억하자.  [ESC] 방으로 돌아가기");
    }
}
