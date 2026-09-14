using System.Collections;
using UnityEngine;

public static class CanvasGroupFadeSequence
{
    public static IEnumerator FadeOut(
        CanvasGroup canvasGroup,
        float delay,
        float duration
    )
    {
        if (canvasGroup == null)
            yield break;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.gameObject.SetActive(false);
    }
}
