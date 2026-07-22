using System;
using UnityEngine;

public enum InventoryMoveMode
{
    Drag = 0,
    ClickToClick = 1
}

public static class InventoryControlSettings
{
    private const string MoveModeKey = "inventory_move_mode";

    public static event Action<InventoryMoveMode> OnMoveModeChanged;

    public static InventoryMoveMode MoveMode
    {
        get
        {
            int saved = PlayerPrefs.GetInt(MoveModeKey, (int)InventoryMoveMode.Drag);
            return Enum.IsDefined(typeof(InventoryMoveMode), saved)
                ? (InventoryMoveMode)saved
                : InventoryMoveMode.Drag;
        }
    }

    public static void SetMoveMode(InventoryMoveMode mode)
    {
        if (MoveMode == mode) return;
        PlayerPrefs.SetInt(MoveModeKey, (int)mode);
        PlayerPrefs.Save();
        OnMoveModeChanged?.Invoke(mode);
    }
}
