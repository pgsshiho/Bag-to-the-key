using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class InvestigationCameraController : MonoBehaviour
{
    public static InvestigationCameraController Instance { get; private set; }

    [Header("Cinemachine")]
    [SerializeField]
    private CinemachineBrain brain;

    [SerializeField]
    private CinemachineCamera defaultCamera;

    [SerializeField]
    private CinemachineCamera investigationCamera;

    private InvestigationPoint activePoint;
    private int idleInvestigationPriority;
    private bool capturedInitialPriority;
    private bool isReturning;

    public bool IsInvestigating => activePoint != null;
    public bool IsTransitioning => brain != null && brain.IsBlending;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CaptureInitialPriority();
    }

    private void Update()
    {
        if (IsInvestigating && Input.GetKeyDown(KeyCode.Escape))
            ReturnToDefault();
    }

    private void OnDisable()
    {
        RestoreInvestigationPriority();
        activePoint = null;
        isReturning = false;
        WorldInteractionGate.Unblock(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool TryFocus(InvestigationPoint point)
    {
        if (point == null || point.ViewPoint == null)
            return false;
        if (IsInvestigating || IsTransitioning || isReturning)
            return false;
        if (!HasRequiredReferences())
        {
            Debug.LogWarning(
                "Investigation camera setup is incomplete. "
                    + "Assign Brain, Default Camera, and Investigation Camera in the Inspector.",
                this
            );
            return false;
        }

        CaptureInitialPriority();
        investigationCamera.transform.SetPositionAndRotation(
            point.ViewPoint.position,
            point.ViewPoint.rotation
        );
        investigationCamera.Lens.FieldOfView = point.FieldOfView;
        investigationCamera.Priority =
            Mathf.Max(defaultCamera.Priority.Value, idleInvestigationPriority) + 1;

        activePoint = point;
        WorldInteractionGate.Block(this);
        return true;
    }

    public bool ReturnToDefault()
    {
        if (!IsInvestigating || IsTransitioning)
            return false;

        RestoreInvestigationPriority();
        activePoint = null;
        isReturning = true;
        StartCoroutine(ReleaseWorldInteractionAfterBlend());
        return true;
    }

    private IEnumerator ReleaseWorldInteractionAfterBlend()
    {
        yield return null;
        while (brain != null && brain.IsBlending)
            yield return null;
        isReturning = false;
        WorldInteractionGate.Unblock(this);
    }

    private bool HasRequiredReferences()
    {
        return brain != null && defaultCamera != null && investigationCamera != null;
    }

    private void CaptureInitialPriority()
    {
        if (capturedInitialPriority || investigationCamera == null)
            return;
        idleInvestigationPriority = investigationCamera.Priority.Value;
        capturedInitialPriority = true;
    }

    private void RestoreInvestigationPriority()
    {
        if (investigationCamera != null && capturedInitialPriority)
            investigationCamera.Priority = idleInvestigationPriority;
    }
}
