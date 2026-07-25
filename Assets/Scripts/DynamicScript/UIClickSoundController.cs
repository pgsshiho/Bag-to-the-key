using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIClickSoundController : MonoBehaviour
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private readonly List<RaycastResult> raycastResults = new();

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0) || EventSystem.current == null) return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            Selectable selectable = result.gameObject.GetComponentInParent<Selectable>();
            if (selectable == null || !selectable.IsInteractable()) continue;

            AudioSource sfxSource = SoundManager.SFX;
            if (sfxSource != null && clickClip != null)
                sfxSource.PlayOneShot(clickClip, volume);
            return;
        }
    }
}
