using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class InventoryControlSettingsDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    private readonly List<InventoryMoveMode> optionModes = new List<InventoryMoveMode>();

    private void Awake()
    {
        if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
        BuildOptions();
    }

    private void OnEnable()
    {
        if (dropdown == null) return;
        if (optionModes.Count == 0) BuildOptions();
        int selectedIndex = optionModes.IndexOf(InventoryControlSettings.MoveMode);
        dropdown.SetValueWithoutNotify(Mathf.Max(0, selectedIndex));
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(SetMoveMode);
    }

    private void OnDisable()
    {
        if (dropdown != null) dropdown.onValueChanged.RemoveListener(SetMoveMode);
    }

    public void SetMoveMode(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= optionModes.Count) return;
        InventoryControlSettings.SetMoveMode(optionModes[optionIndex]);
    }

    private void BuildOptions()
    {
        if (dropdown == null) return;

        optionModes.Clear();
        List<string> optionLabels = new List<string>();
        foreach (InventoryMoveMode mode in Enum.GetValues(typeof(InventoryMoveMode)))
        {
            optionModes.Add(mode);
            optionLabels.Add(mode.ToString());
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(optionLabels);
    }
}
