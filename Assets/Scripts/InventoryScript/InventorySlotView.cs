using UnityEngine;
using UnityEngine.UI;

public enum InventoryPlacementState
{
    None,
    Valid,
    Invalid
}

public class InventorySlotView : MonoBehaviour
{
    public int x;
    public int y;

    [SerializeField] private Color validColor = new Color(0.25f, 0.9f, 0.35f, 0.8f);
    [SerializeField] private Color invalidColor = new Color(0.95f, 0.2f, 0.2f, 0.8f);

    private Image image;
    private Color originalColor;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (image != null) originalColor = image.color;
    }

    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
        name = $"Slot ({x}, {y})";
    }

    public void SetHighlight(InventoryPlacementState state)
    {
        if (image == null) return;

        image.color = state switch
        {
            InventoryPlacementState.Valid => validColor,
            InventoryPlacementState.Invalid => invalidColor,
            _ => originalColor
        };
    }
}
