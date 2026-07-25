using DG.Tweening;
using UnityEngine;

public class MoralityBgmController : MonoBehaviour
{
    [Header("Synchronized BGM Sources")]
    [SerializeField] private AudioSource forwardSource;
    [SerializeField] private AudioSource reversedSource;

    [Header("Balance")]
    [SerializeField, Min(1)] private int balanceAtFullMix = 5;
    [SerializeField, Min(0f)] private float transitionDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float forwardMaxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float reversedMaxVolume = 1f;

    private Tween mixTween;
    private float currentForwardWeight;

    private void OnEnable()
    {
        GameProgressState.MoralityChanged += HandleMoralityChanged;
    }

    private void Start()
    {
        StartSynchronizedSources();
        ApplyBalance(GameProgressState.MoralityBalance, immediate: true);
    }

    private void OnDisable()
    {
        GameProgressState.MoralityChanged -= HandleMoralityChanged;
        mixTween?.Kill();
        mixTween = null;
    }

    private void HandleMoralityChanged(int balance)
    {
        ApplyBalance(balance, immediate: false);
    }

    private void ApplyBalance(int balance, bool immediate)
    {
        float normalized = Mathf.InverseLerp(
            -balanceAtFullMix,
            balanceAtFullMix,
            balance);

        mixTween?.Kill();
        if (immediate || transitionDuration <= 0f)
        {
            currentForwardWeight = normalized;
            ApplyMix(currentForwardWeight);
            mixTween = null;
            return;
        }

        mixTween = DOTween
            .To(
                () => currentForwardWeight,
                value =>
                {
                    currentForwardWeight = value;
                    ApplyMix(value);
                },
                normalized,
                transitionDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .OnComplete(() => mixTween = null);
    }

    private void ApplyMix(float forwardWeight)
    {
        if (forwardSource != null)
            forwardSource.volume = forwardWeight * forwardMaxVolume;
        if (reversedSource != null)
            reversedSource.volume = (1f - forwardWeight) * reversedMaxVolume;
    }

    private void StartSynchronizedSources()
    {
        if (forwardSource == null
            || reversedSource == null
            || forwardSource.clip == null
            || reversedSource.clip == null)
        {
            Debug.LogWarning(
                $"{name}: Assign forward and pre-reversed BGM clips.",
                this);
            return;
        }

        forwardSource.loop = true;
        reversedSource.loop = true;
        forwardSource.time = 0f;
        reversedSource.time = 0f;
        forwardSource.Play();
        reversedSource.Play();
    }
}
