using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RecipeIngredient
{
    public ItemData item;
    public Vector2Int relativePosition;
    public bool requireRotation;
    public bool rotated;

    public bool Matches(ItemInstance instance, Vector2Int anchor)
    {
        if (instance == null || item == null || instance.data != item) return false;
        if (instance.x != anchor.x + relativePosition.x) return false;
        if (instance.y != anchor.y + relativePosition.y) return false;
        return !requireRotation || instance.rotated == rotated;
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
        List<List<ItemInstance>> matches = FindMatches(inventoryItems);
        matchedItems = matches.Count > 0 ? matches[0] : null;
        return matchedItems != null;
    }

    public List<List<ItemInstance>> FindMatches(IReadOnlyList<ItemInstance> inventoryItems)
    {
        List<List<ItemInstance>> matches = new List<List<ItemInstance>>();
        if (inventoryItems == null || ingredients == null || ingredients.Count == 0 || result == null)
            return matches;
        if (ingredients[0].item == null) return matches;

        foreach (ItemInstance anchorItem in inventoryItems)
        {
            if (anchorItem == null || anchorItem.data != ingredients[0].item) continue;

            Vector2Int anchor = new Vector2Int(
                anchorItem.x - ingredients[0].relativePosition.x,
                anchorItem.y - ingredients[0].relativePosition.y);

            List<ItemInstance> candidate = new List<ItemInstance>();
            bool valid = true;

            foreach (RecipeIngredient ingredient in ingredients)
            {
                ItemInstance match = null;
                foreach (ItemInstance inventoryItem in inventoryItems)
                {
                    if (candidate.Contains(inventoryItem)) continue;
                    if (!ingredient.Matches(inventoryItem, anchor)) continue;

                    match = inventoryItem;
                    break;
                }

                if (match == null)
                {
                    valid = false;
                    break;
                }

                candidate.Add(match);
            }

            if (valid) matches.Add(candidate);
        }

        return matches;
    }

    public bool ContainsItem(ItemData item)
    {
        if (item == null) return false;
        foreach (RecipeIngredient ingredient in ingredients)
        {
            if (ingredient.item == item) return true;
        }

        return false;
    }
}
