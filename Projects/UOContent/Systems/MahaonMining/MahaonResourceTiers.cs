using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonMining;

/// <summary>
///     Classic pirate-shard-style weighted resource tables: higher skill unlocks higher
///     tiers and shifts the odds toward them, but never guarantees the top tier even at
///     120 — there's always some chance of pulling a lower grade. Used for both ore and
///     wood, and for both the player's mining pick and bot gathering, so everything pulls
///     from the same table instead of three different ad-hoc pickers.
/// </summary>
public static class MahaonResourceTiers
{
    private static readonly (Type type, double minSkill, double weight, int hue)[] OreTable =
    {
        (typeof(IronOre), 0, 40, 0),
        (typeof(DullCopperOre), 25, 25, 0x453),
        (typeof(ShadowIronOre), 37, 20, 0x455),
        (typeof(CopperOre), 50, 16, 0x459),
        (typeof(BronzeOre), 62, 12, 0x45D),
        (typeof(GoldOre), 75, 9, 0x461),
        (typeof(AgapiteOre), 87, 6, 0x465),
        (typeof(VeriteOre), 99, 4, 0x469),
        (typeof(ValoriteOre), 100, 2, 0x46D)
    };

    // Every ore tier renders as this same graphic — only the hue tells them apart.
    public const int OreGraphic = 0x19B9;

    /// <summary>Creates an ore item of the given tier with the right shared graphic and
    /// tier-specific hue, instead of whatever graphic that vanilla ore class normally has.</summary>
    public static Item CreateOre(Type oreType, int amount)
    {
        var item = (Item)Activator.CreateInstance(oreType, amount);
        item.ItemID = OreGraphic;

        foreach (var (type, _, _, hue) in OreTable)
        {
            if (type == oreType)
            {
                item.Hue = hue;
                break;
            }
        }

        return item;
    }

    private static readonly (Type type, double minSkill, double weight)[] WoodTable =
    {
        (typeof(Log), 0, 40),
        (typeof(OakLog), 20, 22),
        (typeof(AshLog), 40, 16),
        (typeof(YewLog), 60, 12),
        (typeof(HeartwoodLog), 80, 8),
        (typeof(BloodwoodLog), 100, 5),
        (typeof(FrostwoodLog), 110, 3)
    };

    public static Type PickOre(double skill)
    {
        var projected = new (Type type, double minSkill, double weight)[OreTable.Length];
        for (var i = 0; i < OreTable.Length; i++)
        {
            projected[i] = (OreTable[i].type, OreTable[i].minSkill, OreTable[i].weight);
        }

        return PickWeighted(projected, skill);
    }

    public static Type PickWood(double skill) => PickWeighted(WoodTable, skill);

    // 1:1 — CraftResource's metal order (Iron..Valorite) is exactly OreTable's order.
    private const double ToolMatchBonus = 2.0;

    /// <summary>A pick made of iron gives a bonus specifically on iron ore, agapite on
    /// agapite, and so on — 1:1 by tier. Any other tool (or a mismatched metal) gives no
    /// bonus at all, not just a smaller one.</summary>
    public static double OreToolBonus(CraftResource toolResource, Type oreType)
    {
        var toolIndex = (int)toolResource - 1; // Iron=1 in the enum, index 0 in OreTable
        if (toolIndex < 0 || toolIndex >= OreTable.Length)
        {
            return 1.0;
        }

        return OreTable[toolIndex].type == oreType ? ToolMatchBonus : 1.0;
    }

    // Metal has 9 tiers, wood only has 7 — Agapite/Verite/Valorite all point at the best
    // wood (Frostwood) since there's no higher wood tier to give them individually. Every
    // wood species still gets covered by some metal.
    private static readonly int[] MetalToWoodTier = { 0, 1, 2, 3, 4, 5, 6, 6, 6 };

    /// <summary>Same idea as the ore bonus, but wood only has 7 real tiers against metal's
    /// 9, so the top three metals (Agapite/Verite/Valorite) all match Frostwood instead of
    /// each getting their own — every wood species still ends up covered by some metal.</summary>
    public static double WoodToolBonus(CraftResource toolResource, Type woodType)
    {
        var toolIndex = (int)toolResource - 1;
        if (toolIndex < 0 || toolIndex >= MetalToWoodTier.Length)
        {
            return 1.0;
        }

        var woodTier = MetalToWoodTier[toolIndex];
        return WoodTable[woodTier].type == woodType ? ToolMatchBonus : 1.0;
    }

    private static Type PickWeighted((Type type, double minSkill, double weight)[] table, double skill)
    {
        List<(Type type, double weight)> eligible = null;
        double total = 0;

        foreach (var (type, minSkill, weight) in table)
        {
            if (skill < minSkill)
            {
                continue;
            }

            (eligible ??= new List<(Type, double)>()).Add((type, weight));
            total += weight;
        }

        if (eligible == null)
        {
            return table[0].type; // shouldn't happen — everyone qualifies for tier 0
        }

        var roll = Utility.RandomDouble() * total;

        foreach (var (type, weight) in eligible)
        {
            if (roll < weight)
            {
                return type;
            }

            roll -= weight;
        }

        return eligible[^1].type;
    }

    /// <summary>How many units one dig/chop yields — scales with skill, not a flat 1-3.</summary>
    public static int YieldCount(double skill) => Math.Max(1, (int)(skill / 10.0));
}
