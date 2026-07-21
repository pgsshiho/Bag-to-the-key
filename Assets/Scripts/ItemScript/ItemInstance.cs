public class ItemInstance
{
    public ItemData data;
    public bool rotated;
    public string createdByRecipeId;

    public int x;
    public int y;

    public int Width => rotated ? data.height : data.width;
    public int Height => rotated ? data.width : data.height;

    public ItemInstance(ItemData data, string createdByRecipeId = null)
    {
        this.data = data;
        this.createdByRecipeId = createdByRecipeId;
        rotated = false;
    }

    public void Rotate()
    {
        rotated = !rotated;
    }
}
