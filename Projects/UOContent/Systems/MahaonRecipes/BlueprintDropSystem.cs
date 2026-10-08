using ModernUO.CodeGeneratedEvents;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonRecipes;

public static class BlueprintDropSystem
{
    private const double DropChance = 0.04;

    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        var killer = bc.LastKiller is BaseCreature masterCreature
            ? masterCreature.GetDamageMaster(bc)
            : bc.LastKiller;

        if (killer is not PlayerMobile player)
        {
            return;
        }

        if (Utility.RandomDouble() >= DropChance)
        {
            return;
        }

        // Pick a random known recipe ID and skip ones the player already has. Recipe IDs
        // aren't contiguous/dense enough to just RandomElement() the dictionary values
        // cheaply for a single miss-free draw, so try a bounded number of times.
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var recipeId = Utility.RandomMinMax(1, Recipe.LargestRecipeID);

            if (!Recipe.Recipes.TryGetValue(recipeId, out var recipe))
            {
                continue;
            }

            if (player.HasRecipe(recipe))
            {
                continue;
            }

            var blueprint = new MahaonBlueprint(recipeId);

            if (player.Backpack?.TryDropItem(player, blueprint, false) != true)
            {
                blueprint.MoveToWorld(bc.Location, bc.Map);
            }

            player.SendMessage(0x59, $"Выпадает чертёж: {RecipeNameHelper.GetName(recipe)}.");
            return;
        }
    }
}
