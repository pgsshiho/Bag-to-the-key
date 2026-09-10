using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteTextureMixer))]
public sealed class ProgressSpriteTextureMixer : MonoBehaviour
{
    [SerializeField] private OnlyOneUnityString completedProgressId;
    [SerializeField] private bool animateWhenCompleted = true;
    [SerializeField] private GameObject[] hideWhenCompleted =
        System.Array.Empty<GameObject>();

    private SpriteTextureMixer mixer;
    private bool started;
    private bool wasCompleted;

    private void Awake()
    {
        mixer = GetComponent<SpriteTextureMixer>();
    }

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += HandleProgressChanged;
        if (started)
            ApplyCurrentState(immediate: true);
    }

    private void Start()
    {
        started = true;
        ApplyCurrentState(immediate: true);
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= HandleProgressChanged;
    }

    private void HandleProgressChanged()
    {
        if (!started) return;

        bool completed = GameProgressState.IsPuzzleCompleted(completedProgressId);
        if (completed == wasCompleted) return;

        wasCompleted = completed;
        if (completed && animateWhenCompleted)
            mixer.PlayForward(HideCompletionOverlays);
        else if (!completed && animateWhenCompleted)
            mixer.PlayBackward();
        else
        {
            if (completed)
                HideCompletionOverlays();
            mixer.SetBlend(completed ? 1f : 0f);
        }
    }

    private void ApplyCurrentState(bool immediate)
    {
        wasCompleted = GameProgressState.IsPuzzleCompleted(completedProgressId);
        if (wasCompleted)
            HideCompletionOverlays();
        if (immediate)
            mixer.SetBlend(wasCompleted ? 1f : 0f);
        else if (wasCompleted)
            mixer.PlayForward();
        else
            mixer.PlayBackward();
    }

    private void HideCompletionOverlays()
    {
        foreach (GameObject target in hideWhenCompleted)
        {
            if (target != null)
                target.SetActive(false);
        }
    }
}
