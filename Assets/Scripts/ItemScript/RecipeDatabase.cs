using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Recipe Database")]
public class RecipeDatabase : ScriptableObject
{
    [SerializeField] private List<ItemRecipe> recipes = new List<ItemRecipe>();

    public IReadOnlyList<ItemRecipe> Recipes => recipes;

    public ItemRecipe GetById(string recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId)) return null;
        return recipes.Find(recipe => recipe != null && recipe.recipeId == recipeId);
    }

    public List<ItemRecipe> GetRecipesContaining(ItemData item)
    {
        return recipes.FindAll(recipe => recipe != null && recipe.ContainsItem(item));
    }

    public void AddRuntimeRecipes(IEnumerable<ItemRecipe> runtimeRecipes)
    {
        if (runtimeRecipes == null) return;
        foreach (ItemRecipe recipe in runtimeRecipes)
        {
            if (recipe != null && !recipes.Contains(recipe)) recipes.Add(recipe);
        }
    }
}
