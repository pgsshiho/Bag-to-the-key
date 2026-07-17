using UnityEngine;

public class InventoryGrid
{
    public int width;
    public int height;

    private ItemInstance[,] cells;

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
                if (CanPlace(item, x, y))
                {
                    position = new Vector2Int(x, y);
                    return true;
                }
            }
        }

        position = new Vector2Int(-1, -1);
        return false;
    }

    public bool CanPlace(ItemInstance item, int x, int y)
    {
        if (x < 0 || y < 0) return false;
        if (x + item.Width > width) return false;
        if (y + item.Height > height) return false;

        for (int ix = 0; ix < item.Width; ix++)
        {
            for (int iy = 0; iy < item.Height; iy++)
            {
                if (cells[x + ix, y + iy] != null)
                    return false;
            }
        }

        return true;
    }

    public void Place(ItemInstance item, int x, int y)
    {
        if (!CanPlace(item, x, y))
            return;

        item.x = x;
        item.y = y;

        for (int ix = 0; ix < item.Width; ix++)
        {
            for (int iy = 0; iy < item.Height; iy++)
            {
                cells[x + ix, y + iy] = item;
            }
        }
    }

    public void Remove(ItemInstance item)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y] == item)
                    cells[x, y] = null;
            }
        }
    }
}