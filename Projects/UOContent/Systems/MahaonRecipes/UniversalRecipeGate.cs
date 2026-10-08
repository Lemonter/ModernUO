using Server.Engines.Craft;

namespace Server.Systems.MahaonRecipes;

/// <summary>
///     Mahaon's universal blueprint requirement: every craftable item in the game — not
///     just the special AOS/SE runic ones vanilla ModernUO gates — requires a learned
///     blueprint. This reuses the engine's own Recipe/RecipeScroll mechanism
///     (CraftItem.Recipe, PlayerMobile.AcquireRecipe/HasRecipe) rather than inventing a
///     parallel system: we just make sure every CraftItem across every trade skill has a
///     Recipe assigned, where vanilla leaves most of them ungated.
///
///     Must run in Initialize() (post-World, per dev-docs/server-lifecycle.md) — all the
///     Def* craft systems finish registering their CraftItems during Configure(), so we
///     need to run after that's done. Recipe IDs are auto-assigned starting above whatever
///     the highest vanilla-assigned recipe ID is, so we never collide with a real AOS/SE
///     runic recipe ID.
/// </summary>
public static class UniversalRecipeGate
{
    // Must run after every DefBlacksmithy/DefCarpentry/etc. Initialize() has populated its
    // static CraftSystem singleton (those run at the default priority) — otherwise we'd
    // read a null CraftSystem and gate nothing.
    [CallPriority(100)]
    public static void Initialize()
    {
        var nextId = Recipe.LargestRecipeID + 1;

        nextId = GateSystem(DefBlacksmithy.CraftSystem, nextId);
        nextId = GateSystem(DefCarpentry.CraftSystem, nextId);
        nextId = GateSystem(DefTailoring.CraftSystem, nextId);
        nextId = GateSystem(DefTinkering.CraftSystem, nextId);
        nextId = GateSystem(DefAlchemy.CraftSystem, nextId);
        nextId = GateSystem(DefInscription.CraftSystem, nextId);
        nextId = GateSystem(DefCartography.CraftSystem, nextId);
        nextId = GateSystem(DefBowFletching.CraftSystem, nextId);
        nextId = GateSystem(DefCooking.CraftSystem, nextId);
        nextId = GateSystem(DefMasonry.CraftSystem, nextId);
        GateSystem(DefGlassblowing.CraftSystem, nextId);
    }

    private static int GateSystem(CraftSystem system, int nextId)
    {
        if (system == null)
        {
            return nextId;
        }

        foreach (var craftItem in system.CraftItems)
        {
            if (craftItem.Recipe == null)
            {
                craftItem.AddRecipe(nextId++, system);
            }
        }

        return nextId;
    }
}
