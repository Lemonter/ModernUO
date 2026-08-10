using System;
using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonBlueprint : Item
{
    [SerializableField(0)]
    private int _recipeId;

    [Constructible]
    public MahaonBlueprint(int recipeId) : base(0x2831)
    {
        _recipeId = recipeId;
    }

    public Recipe Recipe
    {
        get
        {
            Recipe.Recipes.TryGetValue(_recipeId, out var recipe);
            return recipe;
        }
    }

    public override string DefaultName
    {
        get
        {
            var recipe = Recipe;
            return recipe != null ? $"чертёж: {Systems.MahaonRecipes.RecipeNameHelper.GetName(recipe)}" : "чертёж (неизвестный рецепт)";
        }
    }

    /// <summary>
    ///     Price heuristic: scales with the steepest skill requirement across the item's
    ///     required skills. There's no real "item value" field to key off cleanly across
    ///     every trade skill, so this is our own invented curve — tune freely.
    /// </summary>
    public static int GetPrice(Recipe recipe)
    {
        var craftItem = recipe.CraftItem;

        double maxSkill = 0;
        foreach (var skill in craftItem.Skills)
        {
            if (skill.MinSkill > maxSkill)
            {
                maxSkill = skill.MinSkill;
            }
        }

        // 0 skill -> 5gp floor. 100+ skill -> up toward the 60k ceiling the player recalled.
        var price = 5 + (int)(maxSkill * maxSkill * 6);

        return Math.Clamp(price, 5, 60000);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        var recipe = Recipe;
        if (recipe == null || from is not PlayerMobile pm)
        {
            return;
        }

        if (pm.HasRecipe(recipe))
        {
            from.SendMessage("Ты уже знаешь этот чертёж.");
            return;
        }

        pm.AcquireRecipe(recipe);
        from.SendMessage(0x59, $"Ты изучаешь чертёж: {Systems.MahaonRecipes.RecipeNameHelper.GetName(recipe)}.");
        Delete();
    }
}
