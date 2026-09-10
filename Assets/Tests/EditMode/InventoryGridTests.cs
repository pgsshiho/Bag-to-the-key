using NUnit.Framework;
using UnityEngine;

public class InventoryGridTests
{
    private readonly System.Collections.Generic.List<ItemData> createdItems =
        new System.Collections.Generic.List<ItemData>();

    [TearDown]
    public void TearDown()
    {
        foreach (ItemData item in createdItems)
            Object.DestroyImmediate(item);
        createdItems.Clear();
    }

    [Test]
    public void TryFindSpace_ReturnsFirstAvailableTopLeftCell()
    {
        InventoryGrid grid = new InventoryGrid(3, 2);
        ItemInstance first = new ItemInstance(CreateItem(1, 1));
        ItemInstance candidate = new ItemInstance(CreateItem(1, 1));
        Assert.That(grid.Place(first, 0, 0), Is.True);

        bool found = grid.TryFindSpace(candidate, out Vector2Int position);

        Assert.That(found, Is.True);
        Assert.That(position, Is.EqualTo(new Vector2Int(1, 0)));
    }

    [Test]
    public void TryMove_WhenTargetDoesNotFit_RestoresOriginalPlacement()
    {
        InventoryGrid grid = new InventoryGrid(2, 1);
        ItemInstance item = new ItemInstance(CreateItem(2, 1));
        Assert.That(grid.Place(item, 0, 0), Is.True);

        bool moved = grid.TryMove(item, 1, 0);

        Assert.That(moved, Is.False);
        Assert.That(item.x, Is.EqualTo(0));
        Assert.That(item.y, Is.EqualTo(0));
        Assert.That(grid.GetItemAt(0, 0), Is.SameAs(item));
        Assert.That(grid.GetItemAt(1, 0), Is.SameAs(item));
    }

    [Test]
    public void TryRotate_WhenRotatedShapeDoesNotFit_RestoresOrientationAndCells()
    {
        InventoryGrid grid = new InventoryGrid(2, 1);
        ItemInstance item = new ItemInstance(CreateItem(2, 1));
        Assert.That(grid.Place(item, 0, 0), Is.True);

        bool rotated = grid.TryRotate(item);

        Assert.That(rotated, Is.False);
        Assert.That(item.rotated, Is.False);
        Assert.That(grid.GetItemAt(0, 0), Is.SameAs(item));
        Assert.That(grid.GetItemAt(1, 0), Is.SameAs(item));
    }

    private ItemData CreateItem(int width, int height)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.width = width;
        item.height = height;
        createdItems.Add(item);
        return item;
    }
}
