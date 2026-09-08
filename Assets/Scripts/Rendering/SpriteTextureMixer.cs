using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class SpriteTextureMixTweenEvents
{
    public UnityEvent onStarted = new UnityEvent();
    public UnityEvent onUpdated = new UnityEvent();
    public UnityEvent onCompleted = new UnityEvent();
    public UnityEvent onRewound = new UnityEvent();
    public UnityEvent onKilled = new UnityEvent();
}

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteTextureMixer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite targetSprite;
    [SerializeField, Range(0f, 1f)] private float blend;

    [Header("DOTween Template")]
    [SerializeField, Min(0f)] private float duration = 0.8f;
    [SerializeField] private Ease ease = Ease.InOutSine;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private SpriteTextureMixTweenEvents callbacks =
        new SpriteTextureMixTweenEvents();

    private MaterialPropertyBlock propertyBlock;
    private Tween activeTween;

    public float Blend => blend;
    public Sprite TargetSprite => targetSprite;
    public Tween ActiveTween => activeTween;

    private void Awake()
    {
        EnsureInitialized();
        ApplyProperties();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        ApplyProperties();
    }

    private void OnDisable()
    {
        activeTween?.Kill();
        activeTween = null;
    }

    private void OnValidate()
    {
        blend = Mathf.Clamp01(blend);
        EnsureInitialized();
        ApplyProperties();
    }

    public void SetTargetSprite(Sprite sprite)
    {
        targetSprite = sprite;
        ApplyProperties();
    }

    public void SetBlend(float value)
    {
        blend = Mathf.Clamp01(value);
        ApplyProperties();
    }

    public void SnapToSource()
    {
        activeTween?.Kill();
        activeTween = null;
        SetBlend(0f);
    }

    public void SnapToTarget()
    {
        activeTween?.Kill();
        activeTween = null;
        SetBlend(1f);
    }

    public Tween PlayForward() => TweenTo(1f, duration);

    public Tween PlayBackward() => TweenTo(0f, duration);

    public Tween PlayForward(params TweenCallback[] additionalCompletedCallbacks) =>
        TweenTo(1f, duration, additionalCompletedCallbacks);

    public Tween PlayBackward(params TweenCallback[] additionalCompletedCallbacks) =>
        TweenTo(0f, duration, additionalCompletedCallbacks);

    public Tween TweenTo(
        float targetBlend,
        float seconds,
        params TweenCallback[] additionalCompletedCallbacks)
    {
        EnsureInitialized();
        activeTween?.Kill();

        float destination = Mathf.Clamp01(targetBlend);
        if (seconds <= 0f || Mathf.Approximately(blend, destination))
        {
            SetBlend(destination);
            callbacks.onCompleted?.Invoke();
            InvokeAll(additionalCompletedCallbacks);
            activeTween = null;
            return null;
        }

        activeTween = DOTween
            .To(() => blend, SetBlend, destination, seconds)
            .SetEase(ease)
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnStart(() => callbacks.onStarted?.Invoke())
            .OnUpdate(() => callbacks.onUpdated?.Invoke())
            .OnRewind(() => callbacks.onRewound?.Invoke())
            .OnComplete(() =>
            {
                callbacks.onCompleted?.Invoke();
                InvokeAll(additionalCompletedCallbacks);
                activeTween = null;
            })
            .OnKill(() =>
            {
                callbacks.onKilled?.Invoke();
                activeTween = null;
            });
        return activeTween;
    }

    private void ApplyProperties()
    {
        if (targetRenderer == null
            || targetRenderer.sprite == null
            || targetSprite == null)
        {
            return;
        }

        EnsureInitialized();
        Sprite sourceSprite = targetRenderer.sprite;
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(
            BagToTheKey_SpriteTextureMixShaderIDs.MainTex,
            sourceSprite.texture);
        propertyBlock.SetTexture(
            BagToTheKey_SpriteTextureMixShaderIDs.BlendTex,
            targetSprite.texture);
        propertyBlock.SetVector(
            BagToTheKey_SpriteTextureMixShaderIDs.MainTexRect,
            GetTextureRect(sourceSprite));
        propertyBlock.SetVector(
            BagToTheKey_SpriteTextureMixShaderIDs.BlendTexRect,
            GetTextureRect(targetSprite));
        propertyBlock.SetFloat(
            BagToTheKey_SpriteTextureMixShaderIDs.Blend,
            blend);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void EnsureInitialized()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
        propertyBlock ??= new MaterialPropertyBlock();
    }

    private static Vector4 GetTextureRect(Sprite sprite)
    {
        Rect rect = sprite.textureRect;
        Texture texture = sprite.texture;
        return new Vector4(
            rect.x / texture.width,
            rect.y / texture.height,
            rect.width / texture.width,
            rect.height / texture.height);
    }

    private static void InvokeAll(TweenCallback[] callbacks)
    {
        if (callbacks == null) return;
        foreach (TweenCallback callback in callbacks)
            callback?.Invoke();
    }
}
