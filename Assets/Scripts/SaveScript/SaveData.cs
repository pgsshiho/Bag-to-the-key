using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string sceneName;

    public Vector3Data cameraPosition;
    public Vector3Data cameraRotation;

    public List<InventoryItemSaveData> inventoryItems = new List<InventoryItemSaveData>();
    public List<string> discoveredItemIds = new List<string>();
    public List<string> discoveredRecipeIds = new List<string>();
}

[Serializable]
public class InventoryItemSaveData
{
    public string itemId;
    public int x;
    public int y;
    public bool rotated;
    public string createdByRecipeId;
}

[Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;

    public Vector3Data(Vector3 value)
    {
        x = value.x;
        y = value.y;
        z = value.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}
