using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PointAndClickInteractor : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask interactionMask = ~0;
    [SerializeField] private float maxDistance = 1000f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (FindAnyObjectByType<PointAndClickInteractor>() != null) return;

        GameObject gameObject = new GameObject("PointAndClickInteractor");
        gameObject.AddComponent<PointAndClickInteractor>();
    }

    private void Awake()
    {
        ResolveCamera();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (WorldInteractionGate.IsBlocked) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (targetCamera == null) ResolveCamera();
        if (targetCamera == null) return;

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactionMask))
        {
            if (TryInteract(hit.collider.gameObject)) return;
        }

        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, maxDistance, interactionMask);
        if (hit2D.collider != null)
            TryInteract(hit2D.collider.gameObject);
    }

    private static bool TryInteract(GameObject target)
    {
        MonoBehaviour[] behaviours = target.GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is not IWorldInteractable interactable) continue;
            interactable.Interact();
            return true;
        }

        return false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveCamera();
    }

    private void ResolveCamera()
    {
        targetCamera = Camera.main;
    }
}
