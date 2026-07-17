using UnityEngine;

public class InventorySlotView : MonoBehaviour
{
    public int x;
    public int y;

    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
        name = $"Slot ({x}, {y})";
    }
}