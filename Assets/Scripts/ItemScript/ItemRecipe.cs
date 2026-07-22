using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RecipeIngredient
{
    public ItemData item;
    public Vector2Int relativePosition;

    // Kept for existing recipe assets. The rotated value is now the ingredient's
    // canonical orientation; the whole recipe rotation is applied on top of it.
    public bool requireRotation;
    public bool rotated;
}

public readonly struct RecipeIngredientPlacement
{
    public readonly RecipeIngredient Ingredient;
    public readonly Vector2Int RelativePosition;
    public readonly bool Rotated;
    public readonly int Width;
    public readonly int Height;

    public RecipeIngredientPlacement(
        RecipeIngredient ingredient,
        Vector2Int relativePosition,
        bool rotated,
        int width,
        int height)
    {
        Ingredient = ingredient;
        RelativePosition = relativePosition;
        Rotated = rotated;
        Width = width;
        Height = height;
    }
}

public sealed class ItemRecipeMatch
{
    public IReadOnlyList<ItemInstance> MatchedItems { get; }
    public int QuarterTurns { get; }
    public Vector2Int Origin { get; }
    public Vector2Int Size { get; }

    public ItemRecipeMatch(
        List<ItemInstance> matchedItems,
        int quarterTurns,
        Vector2Int origin,
        Vector2Int size)
    {
        MatchedItems = matchedItems;
        QuarterTurns = Mathf.Abs(quarterTurns) % 4;
        Origin = origin;
        Size = size;
    }
}

[CreateAssetMenu(menuName = "Inventory/Item Recipe")]
public class ItemRecipe : ScriptableObject
{
    public string recipeId;
    public string recipeName;
    public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
    public ItemData result;
    public bool canDisassemble = true;

    public bool TryMatch(IReadOnlyList<ItemInstance> inventoryItems, out List<ItemInstance> matchedItems)
    {
        List<ItemRecipeMatch> matches = FindMatches(inventoryItems);
        matchedItems = matches.Count > 0
            ? new List<ItemInstance>(matches[0].MatchedItems)
            : null;
        return matchedItems != null;
    }

    public List<ItemRecipeMatch> FindMatches(IReadOnlyList<ItemInstance> inventoryItems)
    {
        List<ItemRecipeMatch> matches = new List<ItemRecipeMatch>();
        if (inventoryItems == null || ingredients == null || ingredients.Count == 0 || result == null)
            return matches;
        if (ingredients[0] == null || ingredients[0].item == null) return matches;

        for (int quarterTurns = 0; quarterTurns < 4; quarterTurns++)
        {
            List<RecipeIngredientPlacement> placements = GetPlacements(quarterTurns, out Vector2Int layoutSize);
            if (placements.Count != ingredients.Count) continue;

            RecipeIngredientPlacement firstPlacement = placements[0];
            foreach (ItemInstance anchorItem in inventoryItems)
            {
                if (!MatchesPlacement(anchorItem, firstPlacement, Vector2Int.zero, false)) continue;

                Vector2Int origin = new Vector2Int(
                    anchorItem.x - firstPlacement.RelativePosition.x,
                    anchorItem.y - firstPlacement.RelativePosition.y);

                List<ItemInstance> candidateItems = new List<ItemInstance>();
                bool valid = true;
                foreach (RecipeIngredientPlacement placement in placements)
                {
                    ItemInstance match = null;
                    foreach (ItemInstance inventoryItem in inventoryItems)
                    {
                        if (candidateItems.Contains(inventoryItem)) continue;
                        if (!MatchesPlacement(inventoryItem, placement, origin, true)) continue;
                        match = inventoryItem;
                        break;
                    }

                    if (match == null)
                    {
                        valid = false;
                        break;
                    }

                    candidateItems.Add(match);
                }

                if (!valid || ContainsSameItems(matches, candidateItems)) continue;
                matches.Add(new ItemRecipeMatch(candidateItems, quarterTurns, origin, layoutSize));
            }
        }

        return matches;
    }

    public bool IsMatchValid(ItemRecipeMatch match, IReadOnlyList<ItemInstance> inventoryItems)
    {
        if (match == null || inventoryItems == null || match.MatchedItems.Count != ingredients.Count)
            return false;

        List<RecipeIngredientPlacement> placements = GetPlacements(match.QuarterTurns, out _);
        if (placements.Count != match.MatchedItems.Count) return false;

        for (int i = 0; i < placements.Count; i++)
        {
            ItemInstance item = match.MatchedItems[i];
            if (item == null || !ContainsReference(inventoryItems, item)) return false;
            if (!MatchesPlacement(item, placements[i], match.Origin, true)) return false;
        }

        return true;
    }

    public List<RecipeIngredientPlacement> GetPlacements(int quarterTurns, out Vector2Int layoutSize)
    {
        List<RecipeIngredientPlacement> resultPlacements = new List<RecipeIngredientPlacement>();
        layoutSize = Vector2Int.zero;
        if (ingredients == null || ingredients.Count == 0) return resultPlacements;

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        foreach (RecipeIngredient ingredient in ingredients)
        {
            if (ingredient == null || ingredient.item == null) return new List<RecipeIngredientPlacement>();
            int width = ingredient.rotated ? ingredient.item.height : ingredient.item.width;
            int height = ingredient.rotated ? ingredient.item.width : ingredient.item.height;
            minX = Mathf.Min(minX, ingredient.relativePosition.x);
            minY = Mathf.Min(minY, ingredient.relativePosition.y);
            maxX = Mathf.Max(maxX, ingredient.relativePosition.x + width);
            maxY = Mathf.Max(maxY, ingredient.relativePosition.y + height);
        }

        int canonicalWidth = maxX - minX;
        int canonicalHeight = maxY - minY;
        int normalizedTurns = ((quarterTurns % 4) + 4) % 4;

        foreach (RecipeIngredient ingredient in ingredients)
        {
            int x = ingredient.relativePosition.x - minX;
            int y = ingredient.relativePosition.y - minY;
            int width = ingredient.rotated ? ingredient.item.height : ingredient.item.width;
            int height = ingredient.rotated ? ingredient.item.width : ingredient.item.height;
            int layoutWidth = canonicalWidth;
            int layoutHeight = canonicalHeight;

            for (int turn = 0; turn < normalizedTurns; turn++)
            {
                int oldX = x;
                int oldY = y;
                int oldWidth = width;
                int oldHeight = height;
                x = layoutHeight - (oldY + oldHeight);
                y = oldX;
                width = oldHeight;
                height = oldWidth;

                int previousLayoutWidth = layoutWidth;
                layoutWidth = layoutHeight;
                layoutHeight = previousLayoutWidth;
            }

            bool expectedRotated = ingredient.rotated ^ (normalizedTurns % 2 == 1);
            resultPlacements.Add(new RecipeIngredientPlacement(
                ingredient,
                new Vector2Int(x, y),
                expectedRotated,
                width,
                height));
        }

        layoutSize = normalizedTurns % 2 == 0
            ? new Vector2Int(canonicalWidth, canonicalHeight)
            : new Vector2Int(canonicalHeight, canonicalWidth);
        return resultPlacements;
    }

    public bool ContainsItem(ItemData item)
    {
        if (item == null) return false;
        foreach (RecipeIngredient ingredient in ingredients)
        {
            if (ingredient != null && ingredient.item == item) return true;
        }

        return false;
    }

    private static bool MatchesPlacement(
        ItemInstance instance,
        RecipeIngredientPlacement placement,
        Vector2Int origin,
        bool checkPosition)
    {
        if (instance == null || placement.Ingredient == null) return false;
        if (instance.data != placement.Ingredient.item || instance.rotated != placement.Rotated) return false;
        if (!checkPosition) return true;
        return instance.x == origin.x + placement.RelativePosition.x
            && instance.y == origin.y + placement.RelativePosition.y;
    }

    private static bool ContainsSameItems(List<ItemRecipeMatch> matches, List<ItemInstance> candidate)
    {
        foreach (ItemRecipeMatch match in matches)
        {
            if (match.MatchedItems.Count != candidate.Count) continue;
            bool same = true;
            foreach (ItemInstance item in candidate)
            {
                if (!ContainsReference(match.MatchedItems, item))
                {
                    same = false;
                    break;
                }
            }

            if (same) return true;
        }

        return false;
    }

    private static bool ContainsReference(IReadOnlyList<ItemInstance> items, ItemInstance target)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i], target)) return true;
        }

        return false;
    }
}
