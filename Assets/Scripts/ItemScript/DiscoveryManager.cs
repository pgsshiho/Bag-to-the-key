using System;
using System.Collections.Generic;
using UnityEngine;

public class DiscoveryManager : MonoBehaviour
{
    public static DiscoveryManager Instance { get; private set; }

    public event Action OnDiscoveryChanged;

    [SerializeField] private List<string> discoveredItemIds = new List<string>();
    [SerializeField] private List<string> discoveredRecipeIds = new List<string>();

    private readonly HashSet<string> itemLookup = new HashSet<string>();
    private readonly HashSet<string> recipeLookup = new HashSet<string>();

    public IReadOnlyList<string> DiscoveredItemIds => discoveredItemIds;
    public IReadOnlyList<string> DiscoveredRecipeIds => discoveredRecipeIds;

    public static DiscoveryManager GetOrCreate()
    {
        if (Instance != null) return Instance;

        DiscoveryManager existing = FindAnyObjectByType<DiscoveryManager>();
        if (existing != null) return existing;

        GameObject gameObject = new GameObject("DiscoveryManager");
        return gameObject.AddComponent<DiscoveryManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RebuildLookup();
    }

    public bool DiscoverItem(ItemData item)
    {
        return item != null && DiscoverItem(item.itemId);
    }

    public bool DiscoverItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || !itemLookup.Add(itemId)) return false;
        discoveredItemIds.Add(itemId);
        OnDiscoveryChanged?.Invoke();
        return true;
    }

    public bool DiscoverRecipe(ItemRecipe recipe)
    {
        if (recipe == null || string.IsNullOrWhiteSpace(recipe.recipeId)) return false;
        if (!recipeLookup.Add(recipe.recipeId)) return false;

        discoveredRecipeIds.Add(recipe.recipeId);
        OnDiscoveryChanged?.Invoke();
        return true;
    }

    public bool HasDiscoveredRecipe(string recipeId)
    {
        return !string.IsNullOrWhiteSpace(recipeId) && recipeLookup.Contains(recipeId);
    }

    public bool HasDiscoveredItem(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && itemLookup.Contains(itemId);
    }

    public void Restore(IEnumerable<string> itemIds, IEnumerable<string> recipeIds)
    {
        discoveredItemIds.Clear();
        discoveredRecipeIds.Clear();

        if (itemIds != null) discoveredItemIds.AddRange(itemIds);
        if (recipeIds != null) discoveredRecipeIds.AddRange(recipeIds);

        RebuildLookup();
        OnDiscoveryChanged?.Invoke();
    }

    private void RebuildLookup()
    {
        itemLookup.Clear();
        recipeLookup.Clear();

        foreach (string itemId in discoveredItemIds)
        {
            if (!string.IsNullOrWhiteSpace(itemId)) itemLookup.Add(itemId);
        }

        foreach (string recipeId in discoveredRecipeIds)
        {
            if (!string.IsNullOrWhiteSpace(recipeId)) recipeLookup.Add(recipeId);
        }
    }
}
