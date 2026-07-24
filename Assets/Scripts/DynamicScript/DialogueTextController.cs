using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(TextMeshProUGUI))]
public class DialogueTextController : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private bool useCurrentPositionAsShown = true;
    [SerializeField] private Vector2 shownAnchoredPosition;
    [SerializeField, Min(0f)] private float hiddenPadding = 24f;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float slideDuration = 0.5f;
    [SerializeField] private AnimationCurve slideCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float charactersPerSecond = 35f;
    [SerializeField, Min(0f)] private float hideDelay = 1.5f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

    [Header("Input")]
    [SerializeField] private bool advanceWithLeftClick = true;
    [SerializeField] private bool advanceWithSpaceOrEnter = true;

    [Header("Optional Preview")]
    [SerializeField] private bool playPreviewOnStart = false;
    [SerializeField, TextArea(2, 5)] private List<string> previewLines = new();

    public event Action DialogueStarted;
    public event Action<int, string> LineStarted;
    public event Action DialogueCompleted;

    private readonly List<string> lines = new();
    private RectTransform textRect;
    private TextMeshProUGUI dialogueText;
    private Vector2 hiddenAnchoredPosition;
    private float visibleTextAlpha;
    private Tween slideTween;
    private Tween typingTween;
    private Tween hideTween;
    private Coroutine releaseWorldInteractionRoutine;
    private Action continuationRequested;
    private int currentLineIndex = -1;
    private bool isDialogueActive;
    private bool isSliding;
    private bool isTyping;

    public bool IsDialogueActive => isDialogueActive;
    public bool IsSliding => isSliding;
    public bool IsTyping => isTyping;

    private void Awake()
    {
        textRect = GetComponent<RectTransform>();
        dialogueText = GetComponent<TextMeshProUGUI>();
        visibleTextAlpha = dialogueText.color.a;

        if (useCurrentPositionAsShown)
            shownAnchoredPosition = textRect.anchoredPosition;

        Canvas.ForceUpdateCanvases();
        RecalculateHiddenPosition();
        textRect.anchoredPosition = hiddenAnchoredPosition;
        dialogueText.text = string.Empty;
        dialogueText.maxVisibleCharacters = 0;
    }

    private void Start()
    {
        if (playPreviewOnStart)
            PlayDialogue(previewLines);
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        bool requestedAdvance =
            advanceWithLeftClick && Input.GetMouseButtonDown(0);
        requestedAdvance |=
            advanceWithSpaceOrEnter
            && (Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter));

        if (requestedAdvance)
            Advance();
    }

    private void OnDisable()
    {
        StopCurrentAnimations();
        StopReleaseWorldInteraction();
        isDialogueActive = false;
        continuationRequested = null;
        WorldInteractionGate.Unblock(this);

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = 0;
            SetTextAlpha(visibleTextAlpha);
        }

        if (textRect != null)
            textRect.anchoredPosition = hiddenAnchoredPosition;
    }

    public void PlayDialogue(string line)
    {
        PlayDialogue(line, null);
    }

    public void PlayDialogue(string line, Action onContinuationRequested)
    {
        BeginDialogue(
            new[] { line },
            onContinuationRequested,
            slideIn: true);
    }

    public void PlayDialogue(IEnumerable<string> dialogueLines)
    {
        BeginDialogue(
            dialogueLines,
            onContinuationRequested: null,
            slideIn: true);
    }

    public void ContinueDialogue(
        string line,
        Action onContinuationRequested)
    {
        BeginDialogue(
            new[] { line },
            onContinuationRequested,
            slideIn: false);
    }

    private void BeginDialogue(
        IEnumerable<string> dialogueLines,
        Action onContinuationRequested,
        bool slideIn)
    {
        lines.Clear();
        if (dialogueLines != null)
        {
            foreach (string line in dialogueLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }
        }

        if (lines.Count == 0) return;

        StopCurrentAnimations();
        StopReleaseWorldInteraction();
        SetTextAlpha(visibleTextAlpha);
        WorldInteractionGate.Block(this);
        isDialogueActive = true;
        isSliding = true;
        isTyping = false;
        continuationRequested = onContinuationRequested;
        currentLineIndex = 0;
        dialogueText.text = string.Empty;
        dialogueText.maxVisibleCharacters = 0;

        RecalculateHiddenPosition();
        textRect.anchoredPosition =
            slideIn ? hiddenAnchoredPosition : shownAnchoredPosition;
        DialogueStarted?.Invoke();

        if (slideIn)
        {
            StartSlideIn();
        }
        else
        {
            isSliding = false;
        }

        StartTypingCurrentLine();
    }

    public void Advance()
    {
        if (!isDialogueActive) return;

        if (isTyping)
        {
            CompleteCurrentLineImmediately();
            return;
        }

        if (continuationRequested != null)
        {
            Action continuation = continuationRequested;
            continuationRequested = null;
            continuation.Invoke();
            return;
        }

        hideTween?.Kill();
        hideTween = null;
        dialogueText.text = string.Empty;
        dialogueText.maxVisibleCharacters = 0;
        textRect.anchoredPosition = hiddenAnchoredPosition;
        SetTextAlpha(visibleTextAlpha);
        CompleteDialogue();
    }

    private void StartSlideIn()
    {
        if (slideDuration <= 0f)
        {
            textRect.anchoredPosition = shownAnchoredPosition;
            CompleteSlideIn();
            return;
        }

        slideTween = DOTween
            .To(
                () => textRect.anchoredPosition,
                value => textRect.anchoredPosition = value,
                shownAnchoredPosition,
                slideDuration)
            .SetUpdate(true);

        slideTween.SetEase(
            slideCurve != null ? slideCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f));
        slideTween.OnComplete(CompleteSlideIn);
    }

    private void CompleteSlideIn()
    {
        slideTween = null;
        isSliding = false;
    }

    private void StartTypingCurrentLine()
    {
        typingTween?.Kill();
        typingTween = null;
        hideTween?.Kill();
        hideTween = null;
        SetTextAlpha(visibleTextAlpha);

        string line = lines[currentLineIndex];
        LineStarted?.Invoke(currentLineIndex, line);
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        int characterCount = dialogueText.textInfo.characterCount;

        if (charactersPerSecond <= 0f || characterCount == 0)
        {
            CompleteTyping();
            return;
        }

        isTyping = true;
        float duration = characterCount / charactersPerSecond;
        typingTween = DOTween
            .To(
                () => dialogueText.maxVisibleCharacters,
                value => dialogueText.maxVisibleCharacters = value,
                characterCount,
                duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(CompleteTyping);
    }

    private void CompleteTyping()
    {
        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
        typingTween = null;
        StartHideTween();
    }

    private void CompleteCurrentLineImmediately()
    {
        typingTween?.Kill();
        typingTween = null;

        dialogueText.text = lines[currentLineIndex];
        CompleteTyping();
    }

    private void CompleteDialogue()
    {
        hideTween?.Kill();
        hideTween = null;
        continuationRequested = null;
        isDialogueActive = false;
        releaseWorldInteractionRoutine =
            StartCoroutine(ReleaseWorldInteractionAtEndOfFrame());
        DialogueCompleted?.Invoke();
    }

    private void StartHideTween()
    {
        hideTween?.Kill();

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        if (hideDelay > 0f)
            sequence.AppendInterval(hideDelay);

        if (fadeDuration > 0f)
        {
            sequence.Append(DOTween
                .To(
                    () => dialogueText.color.a,
                    SetTextAlpha,
                    0f,
                    fadeDuration)
                .SetEase(Ease.OutQuad));
        }
        else
        {
            sequence.AppendCallback(() => SetTextAlpha(0f));
        }

        hideTween = sequence.OnComplete(CompleteHide);
    }

    private void CompleteHide()
    {
        hideTween = null;
        dialogueText.text = string.Empty;
        dialogueText.maxVisibleCharacters = 0;
        textRect.anchoredPosition = hiddenAnchoredPosition;
        SetTextAlpha(visibleTextAlpha);
        CompleteDialogue();
    }

    private IEnumerator ReleaseWorldInteractionAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        WorldInteractionGate.Unblock(this);
        releaseWorldInteractionRoutine = null;
    }

    private void RecalculateHiddenPosition()
    {
        float textHeight = textRect != null ? textRect.rect.height : 0f;
        hiddenAnchoredPosition = shownAnchoredPosition
            + Vector2.up * (textHeight + hiddenPadding);
    }

    private void StopCurrentAnimations()
    {
        slideTween?.Kill();
        slideTween = null;
        typingTween?.Kill();
        typingTween = null;
        hideTween?.Kill();
        hideTween = null;

        isSliding = false;
        isTyping = false;
    }

    private void StopReleaseWorldInteraction()
    {
        if (releaseWorldInteractionRoutine == null) return;

        StopCoroutine(releaseWorldInteractionRoutine);
        releaseWorldInteractionRoutine = null;
    }

    private void SetTextAlpha(float alpha)
    {
        Color color = dialogueText.color;
        color.a = alpha;
        dialogueText.color = color;
    }
}
