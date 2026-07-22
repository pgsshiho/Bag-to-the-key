public class ItemInstance
{
    public ItemData data;
    public bool rotated;
    public string createdByRecipeId;
    public int createdByRecipeRotation;

    public int x;
    public int y;

    public int Width => rotated ? data.height : data.width;
    public int Height => rotated ? data.width : data.height;

    public ItemInstance(ItemData data, string createdByRecipeId = null, int createdByRecipeRotation = 0)
    {
        this.data = data;
        this.createdByRecipeId = createdByRecipeId;
        this.createdByRecipeRotation = createdByRecipeRotation;
        rotated = false;
    }

    public void Rotate()
    {
        rotated = !rotated;
    }
}
