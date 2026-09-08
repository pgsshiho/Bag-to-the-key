using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionService : MonoBehaviour
{
    private static SceneTransitionService instance;

    [SerializeField, Min(0f)] private float fadeDuration = 0.45f;
    [SerializeField, Min(0f)] private float titleHoldDuration = 0.75f;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text chapterTitleText;
    private bool isTransitioning;

    public static SceneTransitionService Instance => instance;
    public bool IsTransitioning => isTransitioning;

    public void SetFont(TMP_FontAsset font)
    {
        if (font != null && chapterTitleText != null) chapterTitleText.font = font;
    }

    public static SceneTransitionService GetOrCreate()
    {
        if (instance != null) return instance;

        SceneTransitionService existing =
            FindAnyObjectByType<SceneTransitionService>();
        if (existing != null) return existing;

        SceneTransitionService prefab = Resources.Load<SceneTransitionService>("UI/SceneTransitionService");
        if (prefab == null)
            throw new InvalidOperationException("Missing authored UI prefab: Resources/UI/SceneTransitionService");
        return Instantiate(prefab);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        GetOrCreate();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void OnDestroy()
    {
        WorldInteractionGate.Unblock(this);
        if (instance == this)
            instance = null;
    }

    public bool LoadScene(string sceneName, string chapterTitle = null)
    {
        if (!CanBeginTransition(sceneName))
            return false;

        StartCoroutine(TransitionRoutine(sceneName, chapterTitle));
        return true;
    }

    public IEnumerator LoadSceneAndWait(
        string sceneName,
        string chapterTitle = null)
    {
        if (!CanBeginTransition(sceneName))
            yield break;

        yield return TransitionRoutine(sceneName, chapterTitle);
    }

    public bool StartNewGame(string sceneName, string chapterTitle = null)
    {
        if (isTransitioning)
            return false;

        GameProgressState.Reset();
        DiscoveryManager.GetOrCreate().Restore(
            Array.Empty<string>(),
            Array.Empty<string>());

        InventoryManager inventory = FindAnyObjectByType<InventoryManager>();
        if (inventory != null)
            inventory.Clear();

        return LoadScene(sceneName, chapterTitle);
    }

    private IEnumerator TransitionRoutine(string sceneName, string chapterTitle)
    {
        isTransitioning = true;
        WorldInteractionGate.Block(this);
        canvasGroup.blocksRaycasts = true;
        chapterTitleText.text = chapterTitle ?? string.Empty;

        yield return FadeTo(1f);
        if (!string.IsNullOrWhiteSpace(chapterTitle) && titleHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(titleHoldDuration);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogWarning($"장면 전환을 시작하지 못했습니다: {sceneName}", this);
            yield return FadeTo(0f);
            FinishTransition();
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        yield return null;
        yield return FadeTo(0f);
        FinishTransition();
    }

    private bool CanBeginTransition(string sceneName)
    {
        if (isTransitioning || string.IsNullOrWhiteSpace(sceneName))
            return false;

        if (Application.CanStreamedLevelBeLoaded(sceneName))
            return true;

        Debug.LogWarning(
            $"Build Settings에서 장면을 찾을 수 없습니다: {sceneName}",
            this);
        return false;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalized);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void FinishTransition()
    {
        chapterTitleText.text = string.Empty;
        canvasGroup.blocksRaycasts = false;
        isTransitioning = false;
        WorldInteractionGate.Unblock(this);
    }

}
