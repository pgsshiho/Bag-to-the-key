using DG.Tweening;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private GameObject[] tutorialObjects;
    [SerializeField] private float moveDistance = 100f;
    [SerializeField] private float duration = 0.5f;

    private CanvasGroup[] canvasGroups;
    private RectTransform[] rectTransforms;
    private Vector2[] originalPositions;

    private int currentIndex = 0;
    private bool isFading;

    private void Awake()
    {
        int count = tutorialObjects.Length;

        canvasGroups = new CanvasGroup[count];
        rectTransforms = new RectTransform[count];
        originalPositions = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            canvasGroups[i] = tutorialObjects[i].GetComponent<CanvasGroup>();
            rectTransforms[i] = tutorialObjects[i].GetComponent<RectTransform>();
            originalPositions[i] = rectTransforms[i].anchoredPosition;

            // 모든 튜토리얼을 처음에는 표시
            tutorialObjects[i].SetActive(true);
            canvasGroups[i].alpha = 1f;
        }
    }

    public void NextTutorial()
    {
        // 모든 오브젝트가 사라졌거나, 이전 애니메이션 중이면 무시
        if (isFading || currentIndex >= tutorialObjects.Length)
            return;

        isFading = true;

        int index = currentIndex;

        canvasGroups[index].DOKill();
        rectTransforms[index].DOKill();

        canvasGroups[index].interactable = false;
        canvasGroups[index].blocksRaycasts = false;

        Sequence sequence = DOTween.Sequence();

        // A → B → C 순서로, 각각 아래로 이동하며 사라짐
        sequence.Join(canvasGroups[index].DOFade(0f, duration));
        sequence.Join(
            rectTransforms[index].DOAnchorPosY(
                originalPositions[index].y - moveDistance,
                duration
            )
        );

        sequence.OnComplete(() =>
        {
            tutorialObjects[index].SetActive(false);
            currentIndex++;
            isFading = false;
        });
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            NextTutorial();
        }
    }
}