public class ItemInstance
{
    public ItemData data;
    public bool rotated;

    public int Width => rotated ? data.height : data.width;
    public int Height => rotated ? data.width : data.height;

    public ItemInstance(ItemData data)
    {
        this.data = data;
        rotated = false;
    }

    public void Rotate()
    {
        rotated = !rotated;
    }
}