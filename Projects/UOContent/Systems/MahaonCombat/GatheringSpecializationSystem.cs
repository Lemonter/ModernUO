using System;
using System.Collections.Generic;
using Server.Items;
using Server.Systems.MahaonMetals;

namespace Server.Systems.MahaonCombat;

public enum LumberjackingSpecialization : byte
{
    CommonWood = 0,    // Лесоруб обычного дерева — RegularWood/OakWood (minSkill 0/20)
    UncommonWood = 1,  // Лесоруб необычного дерева — AshWood (minSkill 40)
    RareWood = 2,      // Лесоруб редкого дерева — YewWood (minSkill 60)
    MythicalWood = 3,  // Лесоруб мифического дерева — Heartwood (minSkill 80)
    LegendaryWood = 4  // Лесоруб легендарного дерева — Bloodwood/Frostwood (minSkill 100/110)
}

/// <summary>
///     Deliberately NOT touching the existing MahaonTreeSpeciesTable/MahaonMining
///     systems from earlier sessions — those handle the custom trees/mines themselves
///     (age-based yield, seasonal foliage, swing mechanics) and are flagged for a later
///     full rework. This is a separate, additive layer, not a competing resource-generation
///     system.
///
///     Mining side is keyed by MahaonMetalTier — real ore drops are MahaonOre (the 24-metal
///     system in MahaonMetals/, driven by MahaonMiningSwings), NOT vanilla BaseOre; a first
///     version of this file keyed mining off CraftResource instead, which meant it was wired
///     to a call site (Mining.OnHarvestFinished) that never actually fires in real play —
///     see MahaonMiningSwings.cs's own doc comment: it "replaces the vanilla... behavior"
///     entirely. MahaonMetalTier already exists (MahaonMetal.cs) and is used consistently
///     elsewhere (MahaonMetalCombatEffects, MahaonMetalWearEffects) — reused here instead of
///     inventing a second, competing ore-rarity concept.
///
///     Lumberjacking side stays CraftResource-based: wood really is still Log/CraftResource
///     under MahaonLumberjackingSwings (MahaonResourceTiers.PickWood), so that part was
///     always correctly typed — it was just missing its call site, fixed in
///     MahaonLumberjackingSwings.DoTreeSwing.
///
///     Bonus: chance at an extra unit of whatever was just gathered — same "stackable
///     item, bonus Amount" pattern used for Fishing/Alchemy/Inscription, since raw
///     materials don't have a "quality" number to inflate honestly either.
/// </summary>
public static class GatheringSpecializationSystem
{
    private static readonly Dictionary<(Mobile, MahaonMetalTier), double> MiningValue = new();
    private static readonly Dictionary<(Mobile, LumberjackingSpecialization), double> LumberValue = new();

    public const double MaxValue = 100.0;
    private const double GainChance = 0.15;
    private const double BreadthUnitsPerPercent = 20.0;
    private const int BreadthCap = 10;

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    private static readonly MahaonMetalTier[] AllMiningTiers =
    {
        MahaonMetalTier.Обычный, MahaonMetalTier.Уникальный, MahaonMetalTier.Раритетный, MahaonMetalTier.Мифический
    };

    private static readonly LumberjackingSpecialization[] AllLumberSpecs =
    {
        LumberjackingSpecialization.CommonWood, LumberjackingSpecialization.UncommonWood,
        LumberjackingSpecialization.RareWood, LumberjackingSpecialization.MythicalWood,
        LumberjackingSpecialization.LegendaryWood
    };

    public static double GetMiningValue(Mobile m, MahaonMetalTier tier) =>
        MiningValue.GetValueOrDefault((m, tier), 0.0);

    public static double GetLumberValue(Mobile m, LumberjackingSpecialization spec) =>
        LumberValue.GetValueOrDefault((m, spec), 0.0);

    private static double GetMiningBreadth(Mobile m, MahaonMetalTier active)
    {
        double sum = 0;
        foreach (var t in AllMiningTiers)
        {
            if (t != active)
            {
                sum += GetMiningValue(m, t);
            }
        }

        return Math.Min(BreadthCap, sum / BreadthUnitsPerPercent);
    }

    private static double GetLumberBreadth(Mobile m, LumberjackingSpecialization active)
    {
        double sum = 0;
        foreach (var s in AllLumberSpecs)
        {
            if (s != active)
            {
                sum += GetLumberValue(m, s);
            }
        }

        return Math.Min(BreadthCap, sum / BreadthUnitsPerPercent);
    }

    /// <summary>Rough compatibility mapping for the rare vanilla-typed BaseOre drops
    /// MineComplexSystem's rich-vein fallback still hands out (CreateVeinResource's
    /// OreTiers branch) — everything else in real play is MahaonOre and goes through
    /// MahaonMetalTable's own real Tier field instead of this.</summary>
    public static MahaonMetalTier TierForVanillaOre(CraftResource resource) => resource switch
    {
        CraftResource.Bronze or CraftResource.Gold    => MahaonMetalTier.Уникальный,
        CraftResource.Agapite or CraftResource.Verite => MahaonMetalTier.Раритетный,
        CraftResource.Valorite                        => MahaonMetalTier.Мифический,
        _                                               => MahaonMetalTier.Обычный
    };

    // 1:1 with MahaonResourceTiers.WoodTable's minSkill grouping. Banana/Coconut/Palm all
    // sit at minSkill 102/105/107 — inside the same [100,120] Legendary band as
    // Bloodwood/Frostwood — so they join that band too rather than needing a 6th tier.
    private static LumberjackingSpecialization WoodSpecFor(CraftResource resource) => resource switch
    {
        CraftResource.AshWood   => LumberjackingSpecialization.UncommonWood,
        CraftResource.YewWood   => LumberjackingSpecialization.RareWood,
        CraftResource.Heartwood => LumberjackingSpecialization.MythicalWood,
        CraftResource.Bloodwood or CraftResource.BananaWood or CraftResource.CoconutWood
            or CraftResource.PalmWood or CraftResource.Frostwood => LumberjackingSpecialization.LegendaryWood,
        _ => LumberjackingSpecialization.CommonWood
    };

    /// <summary>Call from MahaonMiningSwings whenever a MahaonOre (or, rarely, a vanilla
    /// BaseOre from MineComplexSystem's vein-reward fallback via TierForVanillaOre) gets
    /// dropped — ore is passed generically as Item since MahaonOre isn't a BaseOre
    /// subclass, but both share Amount for the bonus-unit roll below.</summary>
    public static void OnOreMined(Mobile from, MahaonMetalTier tier, Item ore)
    {
        var key = (from, tier);
        var current = MiningValue.GetValueOrDefault(key, 0.0);

        var cap = MahaonMasteryCapSystem.GetCap(from, RuMiningName(tier));
        if (current < cap && Utility.RandomDouble() <= GainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var newValue = Math.Min(cap, current + gain);
            MiningValue[key] = newValue;
            MahaonSkillTree.NotifyChanged(from);
            MahaonSkillTree.AnnounceGain(from, RuMiningName(tier), newValue - current, newValue);
        }

        var value = GetMiningValue(from, tier);
        var breadth = GetMiningBreadth(from, tier);
        var bonusChance = (value / 100.0 * 20.0 + breadth) / 100.0;

        if (Utility.RandomDouble() < bonusChance)
        {
            ore.Amount += 1;
        }
    }

    /// <summary>Call from Lumberjacking's harvest-finish point whenever harvested is a
    /// Log — same "works for custom Mahaon trees too" reasoning as mining above.</summary>
    public static void OnWoodChopped(Mobile from, Log log)
    {
        var spec = WoodSpecFor(log.Resource);
        var key = (from, spec);
        var current = LumberValue.GetValueOrDefault(key, 0.0);

        if (current < MaxValue && Utility.RandomDouble() <= GainChance)
        {
            var gain = current < 50 ? 0.3 : current < 80 ? 0.15 : 0.05;
            var cap = MahaonMasteryCapSystem.GetCap(from, RuLumberName(spec));
            var newValue = Math.Min(cap, current + gain);
            LumberValue[key] = newValue;
            MahaonSkillTree.NotifyChanged(from);
            MahaonSkillTree.AnnounceGain(from, RuLumberName(spec), newValue - current, newValue);
        }

        var value = GetLumberValue(from, spec);
        var breadth = GetLumberBreadth(from, spec);
        var bonusChance = (value / 100.0 * 20.0 + breadth) / 100.0;

        if (Utility.RandomDouble() < bonusChance)
        {
            log.Amount += 1;
        }
    }

    public static string RuMiningName(MahaonMetalTier tier) => tier switch
    {
        MahaonMetalTier.Обычный    => "Рудокоп обычного металла",
        MahaonMetalTier.Уникальный => "Рудокоп уникального металла",
        MahaonMetalTier.Раритетный => "Рудокоп раритетного металла",
        MahaonMetalTier.Мифический => "Рудокоп мифического металла",
        _                            => tier.ToString()
    };

    public static string RuLumberName(LumberjackingSpecialization spec) => spec switch
    {
        LumberjackingSpecialization.CommonWood    => "Лесоруб обычного дерева",
        LumberjackingSpecialization.UncommonWood  => "Лесоруб необычного дерева",
        LumberjackingSpecialization.RareWood      => "Лесоруб редкого дерева",
        LumberjackingSpecialization.MythicalWood  => "Лесоруб мифического дерева",
        LumberjackingSpecialization.LegendaryWood => "Лесоруб легендарного дерева",
        _                                            => spec.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonGatheringSpecialization", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            MahaonSpecializationPersistenceHelper.Write(writer, MiningValue, (w, key) => w.Write((byte)key));
            MahaonSpecializationPersistenceHelper.Write(writer, LumberValue, (w, key) => w.Write((byte)key));
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version
            MahaonSpecializationPersistenceHelper.Read(reader, MiningValue, r => (MahaonMetalTier)r.ReadByte());
            MahaonSpecializationPersistenceHelper.Read(reader, LumberValue, r => (LumberjackingSpecialization)r.ReadByte());
        }
    }
}
