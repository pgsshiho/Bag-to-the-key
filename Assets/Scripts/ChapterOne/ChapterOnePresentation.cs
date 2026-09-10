using System.Collections;
using TMPro;
using UnityEngine;

public class ChapterOnePresentation : MonoBehaviour
{
    [SerializeField] private DialogueTextController dialogue;
    [SerializeField] private TMP_Text objective;
    [SerializeField] private TMP_Text hint;
    [SerializeField] private CanvasGroup opening;
    [SerializeField] private ChapterFlowController exit;
    [SerializeField] private OnlyOneUnityString introductionId;
    [SerializeField] private OnlyOneUnityString parentGiftId;
    [SerializeField] private OnlyOneUnityString ballFinishedId;
    [SerializeField] private OnlyOneUnityString tableCompletedId;
    [SerializeField] private OnlyOneUnityString booksCompletedId;
    [SerializeField] private OnlyOneUnityString chestCompletedId;
    [SerializeField] private OnlyOneUnityString boxCompletedId;

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
        FixRoomSeams();
        while (SceneTransitionService.Instance != null && SceneTransitionService.Instance.IsTransitioning)
            yield return null;

        WorldInteractionGate.Block(this);
        if (opening != null)
        {
            opening.alpha = 1f;
            opening.blocksRaycasts = true;
            yield return new WaitForSecondsRealtime(1.2f);
            float time = 0f;
            while (time < 1.2f)
            {
                time += Time.unscaledDeltaTime;
                opening.alpha = 1f - Mathf.Clamp01(time / 1.2f);
                yield return null;
            }
            opening.blocksRaycasts = false;
            opening.gameObject.SetActive(false);
        }
        WorldInteractionGate.Unblock(this);
        Refresh();
        if (!GameProgressState.IsPuzzleCompleted(introductionId))
        {
            GameProgressState.CompletePuzzle(introductionId);
            dialogue.PlayDialogue(new[]
            {
                "이 게임은 당신의 창의력을 봅니다. ‘이러면 이러지 않을까?’라는 생각으로 이 방을 풀어나가 주세요.",
                "꼬맹이, 눈을 떴구나. 지금은 빈손이어도 괜찮아. 하나씩 찾아보자.",
                "물건은 클릭해서 살펴보고, 양옆 화살표로 방을 둘러보렴. 저 큰 상자부터 밀어 볼까?",
                "가방 안의 물건은 옮기거나 R로 돌릴 수 있어. 쓸 물건은 가방 오른쪽 위 장착칸에 올려 두렴."
            });
        }
    }

    private void FixRoomSeams()
    {
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.sprite == null
                || (renderer.name != "Wallpaper"
                    && renderer.name != "Floor"
                    && renderer.name != "Skirting"))
            {
                continue;
            }

            Vector3 position = renderer.transform.localPosition;
            position.z = 16f;
            renderer.transform.localPosition = position;

            Vector3 scale = renderer.transform.localScale;
            scale.x = 32f / renderer.sprite.bounds.size.x;
            renderer.transform.localScale = scale;

            if (renderer.name == "Floor")
                renderer.sortingOrder = 20;
            else if (renderer.name == "Skirting")
                renderer.sortingOrder = 30;
        }
    }

    public void Say(string message)
    {
        SetHint(message);
        if (dialogue != null) dialogue.PlayDialogue(new[] { message });
    }

    public void SetHint(string message)
    {
        if (hint != null) hint.text = message;
    }

    public void Refresh()
    {
        if (objective == null) return;
        objective.text = GameProgressState.IsPuzzleCompleted(parentGiftId)
            ? "집에서 문을 열고 나가라  ·  핑크 공을 챙긴 뒤 출구로"
            : "집에서 문을 열고 나가라";
    }

    public void ParentHint()
    {
        string message;
        if (GameProgressState.IsPuzzleCompleted(parentGiftId))
            message = "잘했어, 꼬맹이. 장치 아래의 핑크 공도 챙겼니? 이제 문을 열고 다음 걸음을 내디뎌 보렴.";
        else if (GameProgressState.IsPuzzleCompleted(ballFinishedId))
            message = "고양이 인형을 장착한 뒤 나에게 건네 주렴.";
        else if (GameProgressState.IsPuzzleCompleted(tableCompletedId)
            && GameProgressState.IsPuzzleCompleted(booksCompletedId))
            message = "가방 안에서 길 조각을 A, B, C 순서로 나란히 붙여 봐. 조합 버튼을 누르면 하나의 길이 될 거야.";
        else if (GameProgressState.IsPuzzleCompleted(chestCompletedId))
            message = "테이블에는 갈색 곰이 왼쪽, 키 큰 흰 토끼가 오른쪽이란다. 책들은 장착한 뒤 책장의 빈자리에 놓아 보렴.";
        else if (GameProgressState.IsPuzzleCompleted(boxCompletedId))
            message = "작은 구멍으로 들여다보면, 상자를 열 숫자가 보일지도 몰라.";
        else
            message = "빈손이어도 괜찮아. 오른쪽 큰 상자를 한 번 밀어 보렴.";
        Say(message);
    }

    public void LeaveRoom()
    {
        if (!WorldInteractionGate.IsBlocked) exit.Interact();
    }

}
