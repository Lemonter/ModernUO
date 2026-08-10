using System;
using Server.Items;

namespace Server.Systems.MahaonSeasons;

public enum MahaonTreeSpecies
{
    Apple,
    Peach,
    Pear,
    Oak1,
    Oak2,
    Walnut1,
    Walnut2
}

public readonly struct MahaonTreeSpeciesData
{
    public readonly string NameRu;
    public readonly int TrunkGraphic;
    public readonly int FoliageBareGraphic;
    public readonly int FoliageFruitGraphic; // 0 if this species never bears fruit
    public readonly int FoliageWinterGraphic;
    public readonly Func<int, Item> CreateFruit; // null if this species never bears fruit
    public readonly int FruitMin;
    public readonly int FruitMax;

    public MahaonTreeSpeciesData(
        string nameRu, int trunkGraphic, int foliageBareGraphic, int foliageFruitGraphic,
        int foliageWinterGraphic, Func<int, Item> createFruit, int fruitMin, int fruitMax
    )
    {
        NameRu = nameRu;
        TrunkGraphic = trunkGraphic;
        FoliageBareGraphic = foliageBareGraphic;
        FoliageFruitGraphic = foliageFruitGraphic;
        FoliageWinterGraphic = foliageWinterGraphic;
        CreateFruit = createFruit;
        FruitMin = fruitMin;
        FruitMax = fruitMax;
    }

    public bool BearsFruit => CreateFruit != null;
}

public static class MahaonTreeSpeciesTable
{
    public static readonly MahaonTreeSpeciesData[] Data =
    {
        // Apple
        new(
            "яблоня", 0x0D94, 0x0D95, 0x0D96, 0x0D97,
            amount => new Apple(amount), 40, 60
        ),
        // Peach
        new(
            "персиковое дерево", 0x0D9C, 0x0D9D, 0x0D9E, 0x0D9F,
            amount => new Peach(amount), 40, 60
        ),
        // Pear
        new(
            "груша", 0x0DA4, 0x0DA5, 0x0DA6, 0x0DA7,
            amount => new Pear(amount), 40, 60
        ),
        // Oak 1 — no fruit
        new("дуб", 0x0CDD, 0x0CDE, 0, 0x0CDF, null, 0, 0),
        // Oak 2 — no fruit
        new("дуб", 0x0CDA, 0x0CDB, 0, 0x0CDC, null, 0, 0),
        // Walnut 1 — no fruit
        new("орех", 0x0CE0, 0x0CE1, 0, 0x0CE2, null, 0, 0),
        // Walnut 2 — no fruit
        new("орех", 0x0CE3, 0x0CE4, 0, 0x0CE5, null, 0, 0)
    };

    public static MahaonTreeSpeciesData Get(MahaonTreeSpecies species) => Data[(int)species];

    /// <summary>Matches a real world tree's trunk graphic back to a species — used when
    /// chopping an existing tree to know what kind of sapling it should be able to drop.</summary>
    public static bool TryMatchTrunk(int itemId, out MahaonTreeSpecies species)
    {
        for (var i = 0; i < Data.Length; i++)
        {
            if (Data[i].TrunkGraphic == itemId)
            {
                species = (MahaonTreeSpecies)i;
                return true;
            }
        }

        species = default;
        return false;
    }
}
