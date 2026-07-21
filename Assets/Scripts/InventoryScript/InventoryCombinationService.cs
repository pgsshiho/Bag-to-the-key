using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InventoryCombinationCandidate
{
    public ItemRecipe Recipe { get; }
    public IReadOnlyList<ItemInstance> MatchedItems { get; }

    public InventoryCombinationCandidate(ItemRecipe recipe, List<ItemInstance> matchedItems)
    {
        Recipe = recipe;
        MatchedItems = matchedItems;
    }
}

[RequireComponent(typeof(InventoryManager))]
public class InventoryCombinationService : MonoBehaviour
{
    public event Action<IReadOnlyList<InventoryCombinationCandidate>> OnCandidatesChanged;

    [SerializeField] private RecipeDatabase recipeDatabase;

    private InventoryManager inventoryManager;
    private readonly List<InventoryCombinationCandidate> candidates = new List<InventoryCombinationCandidate>();

    public IReadOnlyList<InventoryCombinationCandidate> Candidates => candidates;
    public RecipeDatabase RecipeDatabase => recipeDatabase;

    private void Awake()
    {
        inventoryManager = GetComponent<InventoryManager>();

        if (recipeDatabase == null)
        {
            recipeDatabase = ScriptableObject.CreateInstance<RecipeDatabase>();
            recipeDatabase.AddRuntimeRecipes(Resources.LoadAll<ItemRecipe>(string.Empty));
        }
    }

    private void OnEnable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged += RefreshCandidates;
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= RefreshCandidates;
    }

    public void RefreshCandidates()
    {
        candidates.Clear();
        if (recipeDatabase != null)
        {
            foreach (ItemRecipe recipe in recipeDatabase.Recipes)
            {
                if (recipe == null) continue;
                foreach (List<ItemInstance> match in recipe.FindMatches(inventoryManager.items))
                    candidates.Add(new InventoryCombinationCandidate(recipe, match));
            }
        }

        OnCandidatesChanged?.Invoke(candidates);
    }

    public bool TryCombine(ItemRecipe recipe)
    {
        if (recipe == null) return false;

        foreach (InventoryCombinationCandidate candidate in candidates)
        {
            if (candidate.Recipe == recipe) return TryCombine(candidate);
        }

        return false;
    }

    public bool TryCombine(InventoryCombinationCandidate candidate)
    {
        if (!IsStillValid(candidate)) return false;

        ItemRecipe recipe = candidate.Recipe;
        List<ItemInstance> matched = new List<ItemInstance>(candidate.MatchedItems);

        List<ItemSnapshot> snapshots = Snapshot(matched);
        foreach (ItemInstance item in matched)
            inventoryManager.RemoveItem(item, false);

        ItemInstance result = new ItemInstance(recipe.result, recipe.recipeId);
        bool placed = inventoryManager.TryAddInstance(result, snapshots[0].x, snapshots[0].y, false)
            || inventoryManager.TryAddInstanceToFirstSpace(result, false);

        if (!placed)
        {
            Restore(snapshots);
            inventoryManager.NotifyChanged();
            return false;
        }

        DiscoveryManager discovery = DiscoveryManager.GetOrCreate();
        discovery.DiscoverItem(recipe.result);
        discovery.DiscoverRecipe(recipe);
        inventoryManager.NotifyChanged();
        return true;
    }

    public bool TryDisassemble(ItemInstance composite)
    {
        if (!CanDisassemble(composite)) return false;

        ItemRecipe recipe = recipeDatabase.GetById(composite.createdByRecipeId);

        ItemSnapshot compositeSnapshot = new ItemSnapshot(composite);
        if (!inventoryManager.RemoveItem(composite, false)) return false;

        List<ItemInstance> createdItems = new List<ItemInstance>();
        bool success = true;

        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            ItemInstance created = new ItemInstance(ingredient.item)
            {
                rotated = ingredient.requireRotation && ingredient.rotated
            };

            int preferredX = compositeSnapshot.x + ingredient.relativePosition.x;
            int preferredY = compositeSnapshot.y + ingredient.relativePosition.y;

            bool placed = inventoryManager.TryAddInstance(created, preferredX, preferredY, false)
                || inventoryManager.TryAddInstanceToFirstSpace(created, false);

            if (!placed)
            {
                success = false;
                break;
            }

            createdItems.Add(created);
        }

        if (!success)
        {
            foreach (ItemInstance created in createdItems)
                inventoryManager.RemoveItem(created, false);

            inventoryManager.TryAddInstance(composite, compositeSnapshot.x, compositeSnapshot.y, false);
            inventoryManager.NotifyChanged();
            return false;
        }

        inventoryManager.NotifyChanged();
        return true;
    }

    public bool CanDisassemble(ItemInstance composite)
    {
        if (composite == null || string.IsNullOrWhiteSpace(composite.createdByRecipeId)) return false;
        ItemRecipe recipe = recipeDatabase != null
            ? recipeDatabase.GetById(composite.createdByRecipeId)
            : null;
        return recipe != null && recipe.canDisassemble;
    }

    public int GetUnknownRecipeCount(ItemData item)
    {
        if (recipeDatabase == null || item == null) return 0;

        DiscoveryManager discovery = DiscoveryManager.GetOrCreate();
        int unknown = 0;
        foreach (ItemRecipe recipe in recipeDatabase.GetRecipesContaining(item))
        {
            if (!discovery.HasDiscoveredRecipe(recipe.recipeId)) unknown++;
        }

        return unknown;
    }

    private static List<ItemSnapshot> Snapshot(IEnumerable<ItemInstance> items)
    {
        List<ItemSnapshot> result = new List<ItemSnapshot>();
        foreach (ItemInstance item in items) result.Add(new ItemSnapshot(item));
        return result;
    }

    private void Restore(IEnumerable<ItemSnapshot> snapshots)
    {
        foreach (ItemSnapshot snapshot in snapshots)
        {
            snapshot.item.rotated = snapshot.rotated;
            inventoryManager.TryAddInstance(snapshot.item, snapshot.x, snapshot.y, false);
        }
    }

    private bool IsStillValid(InventoryCombinationCandidate candidate)
    {
        if (candidate == null || candidate.Recipe == null || candidate.MatchedItems == null)
            return false;

        ItemRecipe recipe = candidate.Recipe;
        if (recipe.ingredients == null || candidate.MatchedItems.Count != recipe.ingredients.Count)
            return false;

        ItemInstance first = candidate.MatchedItems[0];
        if (first == null || !inventoryManager.items.Contains(first)) return false;

        Vector2Int anchor = new Vector2Int(
            first.x - recipe.ingredients[0].relativePosition.x,
            first.y - recipe.ingredients[0].relativePosition.y);

        for (int i = 0; i < recipe.ingredients.Count; i++)
        {
            ItemInstance item = candidate.MatchedItems[i];
            if (item == null || !inventoryManager.items.Contains(item)) return false;
            if (!recipe.ingredients[i].Matches(item, anchor)) return false;
        }

        return true;
    }

    private readonly struct ItemSnapshot
    {
        public readonly ItemInstance item;
        public readonly int x;
        public readonly int y;
        public readonly bool rotated;

        public ItemSnapshot(ItemInstance item)
        {
            this.item = item;
            x = item.x;
            y = item.y;
            rotated = item.rotated;
        }
    }
}
