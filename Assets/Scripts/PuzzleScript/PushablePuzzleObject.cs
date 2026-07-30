using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PuzzleStateController))]
public class PushablePuzzleObject : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private Vector3 localPushOffset = new Vector3(0f, 0f, 1f);
    [SerializeField, Min(1)] private int requiredPushCount = 1;
    [SerializeField, Min(0f)] private float moveDuration = 0.45f;
    [SerializeField] private AnimationCurve moveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private ItemData requiredEquippedItem;
    [SerializeField] private bool consumeRequiredItemOnCompletion;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private UnityEvent onPushStarted;
    [SerializeField] private UnityEvent onPushCompleted;
    [SerializeField] private UnityEvent onPushBlocked;

    private PuzzleStateController completion;
    private Vector3 initialLocalPosition;
    private bool isMoving;

    private void Awake()
    {
        completion = GetComponent<PuzzleStateController>();
        initialLocalPosition = transform.localPosition;
        ResolveInventory();
    }

    private void OnEnable()
    {
        GameProgressState.ProgressChanged += RefreshPersistentPosition;
        RefreshPersistentPosition();
    }

    private void Start()
    {
        RefreshPersistentPosition();
    }

    private void OnDisable()
    {
        GameProgressState.ProgressChanged -= RefreshPersistentPosition;
        StopAllCoroutines();
        isMoving = false;
    }

    public void Interact()
    {
        if (isMoving || completion.IsCompleted)
        {
            onPushBlocked?.Invoke();
            return;
        }

        ResolveInventory();
        if (requiredEquippedItem != null
            && (inventoryManager == null
                || !inventoryManager.IsEquipped(requiredEquippedItem)))
        {
            onPushBlocked?.Invoke();
            return;
        }

        int completedPushes = GetCompletedPushCount();
        if (completedPushes >= requiredPushCount)
        {
            CompletePuzzle();
            return;
        }

        StartCoroutine(PushRoutine(completedPushes + 1));
    }

    private IEnumerator PushRoutine(int pushNumber)
    {
        isMoving = true;
        onPushStarted?.Invoke();

        Vector3 start = transform.localPosition;
        Vector3 target = initialLocalPosition + localPushOffset * pushNumber;
        if (moveDuration <= 0f)
        {
            transform.localPosition = target;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / moveDuration);
                float curved = moveCurve != null
                    ? moveCurve.Evaluate(normalized)
                    : normalized;
                transform.localPosition = Vector3.LerpUnclamped(start, target, curved);
                yield return null;
            }

            transform.localPosition = target;
        }

        isMoving = false;
        GameProgressState.CompletePuzzle(GetPushStateId(pushNumber));
        onPushCompleted?.Invoke();

        if (pushNumber >= requiredPushCount)
            CompletePuzzle();
    }

    private void CompletePuzzle()
    {
        if (consumeRequiredItemOnCompletion && requiredEquippedItem != null)
        {
            ResolveInventory();
            if (inventoryManager == null
                || !inventoryManager.ConsumeEquippedItem(requiredEquippedItem))
            {
                onPushBlocked?.Invoke();
                return;
            }
        }

        completion.Complete();
    }

    private void RefreshPersistentPosition()
    {
        if (isMoving || completion == null) return;

        int completedPushes = completion.IsCompleted
            ? requiredPushCount
            : GetCompletedPushCount();
        transform.localPosition =
            initialLocalPosition + localPushOffset * completedPushes;
    }

    private int GetCompletedPushCount()
    {
        if (completion == null || string.IsNullOrWhiteSpace(completion.PuzzleId))
            return 0;

        int count = 0;
        for (int push = 1; push <= requiredPushCount; push++)
        {
            if (!GameProgressState.IsPuzzleCompleted(GetPushStateId(push)))
                break;
            count++;
        }

        return count;
    }

    private string GetPushStateId(int pushNumber)
    {
        return $"{completion.PuzzleId}.push.{pushNumber}";
    }

    private void ResolveInventory()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();
    }
}
