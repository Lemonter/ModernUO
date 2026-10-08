using System.Collections.Generic;
using System.Linq;
using Server.Engines.Craft;

namespace Server.Systems.MahaonRecipes;

/// <summary>Тир загадочного чертежа = требуемый навык рецепта, поделённый на 6 корзин
/// (0-19.99 → 0, ..., 100-120 → 5, потолок навыка в этом шарде — 120). Считается по
/// максимальному MinSkill среди всех требований CraftItem — если рецепт требует
/// несколько навыков разом, берём самый строгий. Кэшируется один раз при первом
/// обращении — Recipe.Recipes не меняется во время работы сервера (все Recipe создаются
/// при инициализации крафтовых систем, до старта мира).</summary>
public static class MahaonBlueprintTierSystem
{
    private const int TierCount = 6;
    private const double SkillCap = 120.0;

    private static Dictionary<int, int> _tierByRecipeId;
    private static Dictionary<int, List<int>> _recipeIdsByTier;

    private static void EnsureBuilt()
    {
        if (_tierByRecipeId != null)
        {
            return;
        }

        _tierByRecipeId = new Dictionary<int, int>();
        _recipeIdsByTier = new Dictionary<int, List<int>>();

        for (var t = 0; t < TierCount; t++)
        {
            _recipeIdsByTier[t] = new List<int>();
        }

        foreach (var (id, recipe) in Recipe.Recipes)
        {
            var maxSkill = recipe.CraftItem?.Skills.Count > 0
                ? recipe.CraftItem.Skills.Max(s => s.MinSkill)
                : 0.0;

            var tier = (int)System.Math.Clamp(maxSkill / SkillCap * TierCount, 0, TierCount - 1);

            _tierByRecipeId[id] = tier;
            _recipeIdsByTier[tier].Add(id);
        }
    }

    public static int GetTier(Recipe recipe)
    {
        EnsureBuilt();
        return _tierByRecipeId.GetValueOrDefault(recipe.ID, 0);
    }

    /// <summary>Случайный ещё не выученный рецепт из указанного тира — null, если игрок
    /// уже знает всё в этом тире ("ничего нового").</summary>
    public static Recipe PickUnknownInTier(Mobiles.PlayerMobile player, int tier)
    {
        EnsureBuilt();

        var ids = _recipeIdsByTier.GetValueOrDefault(tier);

        if (ids == null || ids.Count == 0)
        {
            return null;
        }

        var unknown = new List<int>();

        foreach (var id in ids)
        {
            if (!player.HasRecipe(id))
            {
                unknown.Add(id);
            }
        }

        if (unknown.Count == 0)
        {
            return null;
        }

        var pickedId = unknown[Utility.Random(unknown.Count)];
        return Recipe.Recipes.GetValueOrDefault(pickedId);
    }
}
