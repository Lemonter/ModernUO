namespace Server.Systems.MahaonProfessions;

/// <summary>
///     Called from the base HarvestSystem.Give — the one shared chokepoint all three
///     harvest skills (Mining/Lumberjacking/Fishing) funnel through when actually handing
///     a resource item to whoever just harvested it. Keeps the craftsman perk in one place
///     instead of three separate per-skill overrides.
/// </summary>
public static class ProfessionHarvestBonus
{
    private const double DoubleYieldChance = 0.2;

    public static void ApplyCraftsmanBonus(Mobile m, Item item)
    {
        if (item.Amount <= 0)
        {
            return;
        }

        var profession = ProfessionSystem.GetProfession(m);
        if (profession == null || ProfessionData.All[profession.Value].Category != ProfessionCategory.Craft)
        {
            return;
        }

        if (Utility.RandomDouble() < DoubleYieldChance)
        {
            item.Amount *= 2;
        }
    }
}
