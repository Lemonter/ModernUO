using Server.Engines.Craft;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     "Рациональное использование" — universal for every craft skill, not another
///     specialization system to train separately. Scales directly off the crafter's REAL
///     vanilla skill value in whatever governs the current CraftSystem (craftSystem.
///     MainSkill) — no separate tracked number, so this works immediately for every craft
///     skill including the ones that don't have a specialization system yet (Cartography,
///     Imbuing) without waiting on their full rework.
///
///     Hooked into CraftItem.ConsumeRes, right before resources are actually deducted —
///     each REQUIRED unit of a resource gets an independent roll to not be needed, so
///     higher-amount crafts (which need more raw material) get proportionally more
///     chances to save something, not just one flat roll per craft.
/// </summary>
public static class MaterialEconomySystem
{
    private const double MaxSaveChancePerUnit = 0.20; // one roll per required unit, up to 20% each at 100 skill

    /// <summary>Rolls once per unit of `amount` currently required, returns how many units
    /// can be skipped this time (amount is never reduced below 1 total).</summary>
    public static int GetSavedUnits(Mobile from, CraftSystem craftSystem, int amount)
    {
        if (amount <= 1)
        {
            return 0; // always need at least 1 unit — economy never fully eliminates a craft
        }

        var skillValue = from.Skills[craftSystem.MainSkill].Value;
        var chancePerUnit = skillValue / 100.0 * MaxSaveChancePerUnit;

        var saved = 0;

        for (var i = 0; i < amount - 1; i++) // never risk rolling the last required unit away
        {
            if (Utility.RandomDouble() < chancePerUnit)
            {
                saved++;
            }
        }

        return saved;
    }
}
