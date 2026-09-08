using System.Collections;
using DG.Tweening;
using UnityEngine;

public class ChapterOneBallRun : MonoBehaviour
{
    [SerializeField] private Transform ball;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float duration = 4f;
    [SerializeField, Min(0f)] private float holeExitDuration = .45f;
    [SerializeField, Range(0f, 1f)] private float holeScaleMultiplier = .05f;
    [SerializeField] private Ease holeExitEase = Ease.InCubic;
    [SerializeField] private ChapterOnePresentation presentation;
    [SerializeField] private OnlyOneUnityString trackInstalledId;
    [SerializeField] private OnlyOneUnityString ballFinishedId;
    private Coroutine routine;
    private Tween holeExitTween;
    private SpriteRenderer ballRenderer;
    private Vector3 initialBallScale;
    private Color initialBallColor;

    private void Awake()
    {
        if (ball == null) return;
        ballRenderer = ball.GetComponent<SpriteRenderer>();
        initialBallScale = ball.localScale;
        if (ballRenderer != null)
            initialBallColor = ballRenderer.color;
    }

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += Refresh;
        Refresh();
    }

    private void Start() => Refresh();

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= Refresh;
        if (routine != null) StopCoroutine(routine);
        holeExitTween?.Kill();
        holeExitTween = null;
        routine = null;
        WorldInteractionGate.Unblock(this);
    }

    private void Refresh()
    {
        if (ball == null || waypoints == null || waypoints.Length < 2) return;
        if (!GameProgressState.IsPuzzleCompleted(trackInstalledId))
        {
            if (routine != null) StopCoroutine(routine);
            holeExitTween?.Kill();
            holeExitTween = null;
            routine = null;
            ball.position = waypoints[0].position;
            ResetBallVisual();
            WorldInteractionGate.Unblock(this);
            return;
        }
        if (GameProgressState.IsPuzzleCompleted(ballFinishedId))
        {
            ball.position = waypoints[waypoints.Length - 1].position;
            HideBallVisual();
            return;
        }
        if (GameProgressState.IsPuzzleCompleted(trackInstalledId) && routine == null)
            routine = StartCoroutine(Roll());
    }

    private IEnumerator Roll()
    {
        WorldInteractionGate.Block(this);
        ball.position = waypoints[0].position;
        ResetBallVisual();
        presentation.SetHint("조각들이 하나의 길이 되었어. 공이 끝까지 갈 수 있을까?");
        // Resuming after a save during the animation replays this transient motion.
        float segmentDuration = Mathf.Max(0.01f, duration / (waypoints.Length - 1));
        for (int i = 1; i < waypoints.Length; i++)
        {
            float elapsed = 0f;
            while (elapsed < segmentDuration)
            {
                elapsed += Time.deltaTime;
                ball.position = Vector3.Lerp(waypoints[i - 1].position,
                    waypoints[i].position, Mathf.Clamp01(elapsed / segmentDuration));
                yield return null;
            }
        }

        holeExitTween = CreateHoleExitTween();
        if (holeExitTween != null)
            yield return holeExitTween.WaitForCompletion();
        holeExitTween = null;

        GameProgressState.CompletePuzzle(ballFinishedId);
        WorldInteractionGate.Unblock(this);
        routine = null;
        presentation.Say("꼬맹이, 해냈구나. 장치 아래의 고양이 인형을 가져와 줄래?");
    }

    private Tween CreateHoleExitTween()
    {
        float tweenDuration = Mathf.Max(0f, holeExitDuration);
        Vector3 hiddenScale = initialBallScale * holeScaleMultiplier;
        if (tweenDuration <= 0f)
        {
            ball.localScale = hiddenScale;
            SetBallAlpha(0f);
            return null;
        }

        Sequence sequence = DOTween.Sequence()
            .Join(ball.DOScale(hiddenScale, tweenDuration));
        if (ballRenderer != null)
            sequence.Join(ballRenderer.DOFade(0f, tweenDuration));
        return sequence
            .SetEase(holeExitEase)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    private void ResetBallVisual()
    {
        ball.localScale = initialBallScale;
        if (ballRenderer != null)
            ballRenderer.color = initialBallColor;
    }

    private void HideBallVisual()
    {
        ball.localScale = initialBallScale * holeScaleMultiplier;
        SetBallAlpha(0f);
    }

    private void SetBallAlpha(float alpha)
    {
        if (ballRenderer == null) return;
        Color color = ballRenderer.color;
        color.a = alpha;
        ballRenderer.color = color;
    }
}
