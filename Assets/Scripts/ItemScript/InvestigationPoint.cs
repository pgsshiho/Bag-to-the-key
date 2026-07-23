using UnityEngine;

public class InvestigationPoint : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private InvestigationCameraController cameraController;
    [SerializeField] private Transform viewPoint;
    [SerializeField, Range(1f, 179f)] private float fieldOfView = 35f;

    public Transform ViewPoint => viewPoint;
    public float FieldOfView => fieldOfView;

    private void Awake()
    {
        ResolveViewPoint();
    }

    private void Reset()
    {
        ResolveViewPoint();
    }

    private void OnValidate()
    {
        fieldOfView = Mathf.Clamp(fieldOfView, 1f, 179f);
        ResolveViewPoint();
    }

    public void Interact()
    {
        if (viewPoint == null)
        {
            Debug.LogWarning(
                $"{name}: InvestigationViewPoint is not assigned.",
                this);
            return;
        }

        if (cameraController == null)
        {
            Debug.LogWarning(
                $"{name}: InvestigationCameraController is not assigned.",
                this);
            return;
        }

        cameraController.TryFocus(this);
    }

    private void ResolveViewPoint()
    {
        if (viewPoint != null) return;
        Transform child = transform.Find("InvestigationViewPoint");
        if (child != null) viewPoint = child;
    }
}
