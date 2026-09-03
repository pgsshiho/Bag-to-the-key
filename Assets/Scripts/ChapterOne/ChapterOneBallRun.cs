using System.Collections;
using UnityEngine;

public class ChapterOneBallRun : MonoBehaviour
{
    [SerializeField] private Transform ball;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float duration = 4f;
    [SerializeField] private ChapterOnePresentation presentation;
    private Coroutine routine;

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
        routine = null;
        WorldInteractionGate.Unblock(this);
    }

    private void Refresh()
    {
        if (ball == null || waypoints == null || waypoints.Length < 2) return;
        if (!GameProgressState.IsPuzzleCompleted("ch01.track_installed"))
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            ball.position = waypoints[0].position;
            WorldInteractionGate.Unblock(this);
            return;
        }
        if (GameProgressState.IsPuzzleCompleted("ch01.ball_finished"))
        {
            ball.position = waypoints[waypoints.Length - 1].position;
            return;
        }
        if (GameProgressState.IsPuzzleCompleted("ch01.track_installed") && routine == null)
            routine = StartCoroutine(Roll());
    }

    private IEnumerator Roll()
    {
        WorldInteractionGate.Block(this);
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
        GameProgressState.CompletePuzzle("ch01.ball_finished");
        WorldInteractionGate.Unblock(this);
        routine = null;
        presentation.Say("꼬맹이, 해냈구나. 장치 아래의 고양이 인형을 가져와 줄래?");
    }
}
