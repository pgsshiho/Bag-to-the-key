using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class TransformTweeningComponent : MonoBehaviour
{
    [Flags]
    private enum TransformTweeningFlags
    {
        None = 0,
        Position = 1 << 0,
        Rotation = 1 << 1,
        Scale = 1 << 2,
    }

    private enum CallbackType
    {
        OnAwake,
        OnStart,
        BasedEvent,
    }

    [SerializeField]
    private CallbackType _callbackType;

    [SerializeField]
    private Ease _easeType = Ease.Linear;

    [SerializeField]
    private TransformTweeningFlags _tweeningFlags;

    [SerializeField, NaughtyAttributes.ShowIf("IsPositionTweeningEnabled")]
    private Vector3 _targetPosition;

    [SerializeField, NaughtyAttributes.ShowIf("IsRotationTweeningEnabled")]
    private Vector3 _targetRotation;

    [SerializeField, NaughtyAttributes.ShowIf("IsScaleTweeningEnabled")]
    private Vector3 _targetScale;

    [SerializeField]
    private float _duration = 1f;

    [SerializeField]
    private UnityEvent _onStart;

    [SerializeField]
    private UnityEvent _onUpdate;

    [SerializeField]
    private UnityEvent _onComplete;

    private Sequence _tweenSequence;

    #region
    private bool IsPositionTweeningEnabled =>
        (_tweeningFlags & TransformTweeningFlags.Position) != 0;
    private bool IsRotationTweeningEnabled =>
        (_tweeningFlags & TransformTweeningFlags.Rotation) != 0;
    private bool IsScaleTweeningEnabled => (_tweeningFlags & TransformTweeningFlags.Scale) != 0;
    #endregion

    private void Awake()
    {
        _tweenSequence = DOTween.Sequence();
        _tweenSequence.SetAutoKill(false);
        _tweenSequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        _tweenSequence.Pause();

        if (IsPositionTweeningEnabled)
            _tweenSequence.Append(transform.DOMove(_targetPosition, _duration).SetEase(_easeType));

        if (IsRotationTweeningEnabled)
            _tweenSequence.Join(transform.DORotate(_targetRotation, _duration).SetEase(_easeType));

        if (IsScaleTweeningEnabled)
            _tweenSequence.Join(transform.DOScale(_targetScale, _duration).SetEase(_easeType));

        _tweenSequence.OnStart(() => _onStart?.Invoke());
        _tweenSequence.OnUpdate(() => _onUpdate?.Invoke());
        _tweenSequence.OnComplete(() => _onComplete?.Invoke());

        if (_callbackType == CallbackType.OnAwake)
        {
            TweeningExecute();
        }
    }

    private void Start()
    {
        if (_callbackType == CallbackType.OnStart)
        {
            TweeningExecute();
        }
    }

    public void TweeningExecute()
    {
        if (_tweenSequence != null && _tweenSequence.IsActive())
            _tweenSequence.Restart();
    }

    private void OnDestroy()
    {
        _tweenSequence?.Kill();
        _tweenSequence = null;
    }
}
