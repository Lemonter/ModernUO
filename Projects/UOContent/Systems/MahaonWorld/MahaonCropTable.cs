using System;
using Server.Items;
using Server.Systems.MahaonSeasons;

namespace Server.Systems.MahaonWorld;

public enum MahaonCropType
{
    Onion,
    Garlic
}

public readonly struct MahaonCropDefinition
{
    public readonly string NameRu;
    public readonly int SpringGraphic;
    public readonly int SummerGraphic;
    public readonly int AutumnGraphic; // ripe — this is the one that's actually harvestable
    public readonly Func<Item> CreateHarvest;

    public MahaonCropDefinition(string nameRu, int springGraphic, int summerGraphic, int autumnGraphic, Func<Item> createHarvest)
    {
        NameRu = nameRu;
        SpringGraphic = springGraphic;
        SummerGraphic = summerGraphic;
        AutumnGraphic = autumnGraphic;
        CreateHarvest = createHarvest;
    }

    public int GraphicFor(MahaonSeason season) => season switch
    {
        MahaonSeason.Spring => SpringGraphic,
        MahaonSeason.Summer => SummerGraphic,
        MahaonSeason.Autumn => AutumnGraphic,
        _                   => AutumnGraphic // Winter is handled separately (tile goes invisible)
    };
}

/// <summary>Add new crops here — one line each, everything else (season cycling, harvest,
/// yearly reset) is generic and reads from this table.</summary>
public static class MahaonCropTable
{
    public static readonly MahaonCropDefinition[] Data =
    {
        new("Лук", 0x0C68, 0x0C69, 0x0C6F, () => new Onion()),

        // Real Garlic's default look is already 0xF84 — no ItemID override needed, this
        // is just the class as-is (correct name, weight, reagent behavior, all of it).
        new("Чеснок", 0x0C68, 0x0C69, 0x18E1, () => new Garlic())
    };
}
