using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ExclusivePuzzleChoiceOption : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private ExclusivePuzzleChoice choice;
    [SerializeField] private string optionId;
    [SerializeField] private string outcomeId;
    [SerializeField] private int moralityDelta;
    [SerializeField] private ItemData requiredEquippedItem;
    [SerializeField] private bool consumeRequiredItem = true;
    [SerializeField] private List<ItemData> rewardItems = new List<ItemData>();
    [SerializeField] private GameObject availableVisual;
    [SerializeField] private GameObject selectedVisual;
    [SerializeField] private GameObject unavailableVisual;
    [SerializeField] private UnityEvent onSelectedStateApplied;
    [SerializeField] private UnityEvent onUnavailableStateApplied;
    [SerializeField] private UnityEvent onFirstSelected;
    [SerializeField] private UnityEvent onMissingItem;
    [SerializeField] private UnityEvent onInventoryFull;

    private int appliedState = -1;

    public int MoralityDelta => moralityDelta;
    public ItemData RequiredEquippedItem => requiredEquippedItem;
    public bool ConsumeRequiredItem => consumeRequiredItem;
    public IReadOnlyList<ItemData> RewardItems => rewardItems;

    private void Awake()
    {
        if (choice == null)
            choice = GetComponentInParent<ExclusivePuzzleChoice>();
    }

    public void Interact()
    {
        if (choice == null)
            choice = GetComponentInParent<ExclusivePuzzleChoice>();
        choice?.TryChoose(this);
    }

    public string ResolveOutcomeId(string choicePuzzleId)
    {
        if (!string.IsNullOrWhiteSpace(outcomeId))
            return outcomeId;

        string resolvedOptionId = !string.IsNullOrWhiteSpace(optionId)
            ? optionId
            : name;
        return $"{choicePuzzleId}.choice.{resolvedOptionId}";
    }

    public void SetChoiceState(ExclusivePuzzleChoiceOption selected)
    {
        bool isSelected = selected == this;
        bool isUnavailable = selected != null && !isSelected;
        int state = isSelected ? 1 : isUnavailable ? 2 : 0;

        if (availableVisual != null)
            availableVisual.SetActive(state == 0);
        if (selectedVisual != null)
            selectedVisual.SetActive(isSelected);
        if (unavailableVisual != null)
            unavailableVisual.SetActive(isUnavailable);

        if (appliedState == state) return;
        appliedState = state;
        if (isSelected)
            onSelectedStateApplied?.Invoke();
        else if (isUnavailable)
            onUnavailableStateApplied?.Invoke();
    }

    public void ApplySelectedState()
    {
        SetChoiceState(this);
    }

    public void NotifyFirstSelected()
    {
        onFirstSelected?.Invoke();
    }

    public void NotifyUnavailable()
    {
        onUnavailableStateApplied?.Invoke();
    }

    public void NotifyMissingItem()
    {
        onMissingItem?.Invoke();
    }

    public void NotifyInventoryFull()
    {
        onInventoryFull?.Invoke();
    }
}
