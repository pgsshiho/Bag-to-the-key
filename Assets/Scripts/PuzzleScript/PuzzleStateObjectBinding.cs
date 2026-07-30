using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PuzzleStateController))]
public class PuzzleStateObjectBinding : MonoBehaviour
{
    [SerializeField] private List<GameObject> activeWhileIncomplete =
        new List<GameObject>();
    [SerializeField] private List<GameObject> activeWhenCompleted =
        new List<GameObject>();

    private PuzzleStateController completion;

    private void Awake()
    {
        completion = GetComponent<PuzzleStateController>();
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

    public void Refresh()
    {
        if (completion == null)
            completion = GetComponent<PuzzleStateController>();

        bool completed = completion.IsCompleted;
        SetActive(activeWhileIncomplete, !completed);
        SetActive(activeWhenCompleted, completed);
    }

    private static void SetActive(IEnumerable<GameObject> targets, bool active)
    {
        foreach (GameObject target in targets)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
