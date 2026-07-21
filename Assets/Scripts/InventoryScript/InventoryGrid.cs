using UnityEngine;

public class InventoryGrid
{
    public int width;
    public int height;

    private readonly ItemInstance[,] cells;

    public InventoryGrid(int width, int height)
    {
        this.width = width;
        this.height = height;
        cells = new ItemInstance[width, height];
    }

    public bool TryFindSpace(ItemInstance item, out Vector2Int position)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!CanPlace(item, x, y)) continue;

                position = new Vector2Int(x, y);
                return true;
            }
        }

        position = new Vector2Int(-1, -1);
        return false;
    }

    public bool CanPlace(ItemInstance item, int x, int y)
    {
        return CanPlace(item, x, y, null);
    }

    public bool CanPlace(ItemInstance item, int x, int y, ItemInstance ignoredItem)
    {
        if (item == null || item.data == null) return false;
        if (x < 0 || y < 0) return false;
        if (x + item.Width > width || y + item.Height > height) return false;

        for (int ix = 0; ix < item.Width; ix++)
        {
            for (int iy = 0; iy < item.Height; iy++)
            {
                ItemInstance occupant = cells[x + ix, y + iy];
                if (occupant != null && occupant != ignoredItem)
                    return false;
            }
        }

        return true;
    }

    public bool Place(ItemInstance item, int x, int y)
    {
        if (!CanPlace(item, x, y)) return false;

        item.x = x;
        item.y = y;

        for (int ix = 0; ix < item.Width; ix++)
        {
            for (int iy = 0; iy < item.Height; iy++)
                cells[x + ix, y + iy] = item;
        }

        return true;
    }

    public void Remove(ItemInstance item)
    {
        if (item == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y] == item)
                    cells[x, y] = null;
            }
        }
    }

    public bool TryMove(ItemInstance item, int targetX, int targetY)
    {
        if (item == null) return false;

        int originalX = item.x;
        int originalY = item.y;

        Remove(item);
        if (Place(item, targetX, targetY)) return true;

        Place(item, originalX, originalY);
        return false;
    }

    public bool TryRotate(ItemInstance item)
    {
        if (item == null) return false;

        int originalX = item.x;
        int originalY = item.y;

        Remove(item);
        item.Rotate();

        if (Place(item, originalX, originalY)) return true;

        item.Rotate();
        Place(item, originalX, originalY);
        return false;
    }

    public ItemInstance GetItemAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        return cells[x, y];
    }
}
