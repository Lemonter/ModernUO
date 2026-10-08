using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Mobiles;
using Server.Systems.MahaonRecipes;

namespace Server.Items;

/// <summary>Загадочный чертёж — тир виден сразу (0-5, по требуемому навыку рецепта), но
/// какой конкретно рецепт содержится — неизвестно до использования. При открытии
/// случайно выбирает ещё не выученный рецепт этого тира и обучает СРАЗУ, даже если
/// навыка не хватает для самого крафта (в отличие от ванильного RecipeScroll, у которого
/// есть проверка GetSuccessChance/allRequiredSkills — тут её намеренно нет). Если всё в
/// тире уже выучено — чертёж просто выбрасывается ("ничего нового").</summary>
[SerializationGenerator(0, false)]
public partial class MahaonMysteryBlueprint : Item
{
    [SerializableField(0)]
    private int _tier;

    [Constructible]
    public MahaonMysteryBlueprint(int tier = 0) : base(0x2831)
    {
        _tier = System.Math.Clamp(tier, 0, 5);
        Name = $"загадочный чертёж (тир {_tier})";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        if (from is not PlayerMobile player)
        {
            return;
        }

        var recipe = MahaonBlueprintTierSystem.PickUnknownInTier(player, _tier);

        if (recipe == null)
        {
            player.SendMessage(0x59, "Ничего нового — ты уже знаешь все рецепты этого тира. Чертёж выброшен.");
            Delete();
            return;
        }

        var craftItem = recipe.CraftItem;

        if (craftItem?.NameString != null)
        {
            // Большинство рецептов на этом шарде уже названы по-русски напрямую строкой
            // — используем её как есть, без похода через клило вообще.
            player.SendMessage(0x59, $"Ты изучаешь новый рецепт: {craftItem.NameString}.");
        }
        else
        {
            // Резерв для рецептов без строкового имени (только номер клило,
            // видимо, оставшийся англоязычный ванильный) — тот же паттерн, что и у
            // ванильного RecipeScroll: аргумент, начинающийся с "#", клиент разрешает
            // как вложенную cliloc-ссылку.
            player.SendLocalizedMessage(1073451, recipe.TextDefinition.ToString()); // You have learned a new recipe: ~1_RECIPE~
        }

        player.AcquireRecipe(recipe);
        Delete();
    }
}
