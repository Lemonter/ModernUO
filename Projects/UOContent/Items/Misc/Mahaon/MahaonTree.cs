using System;
using ModernUO.Serialization;
using Server.Systems.MahaonMining;
using Server.Systems.MahaonSeasons;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonTree : Item
{
    [SerializableField(0)]
    private MahaonTreeSpecies _species;

    [SerializableField(1)]
    private MahaonTreeFoliage _foliage;

    [SerializableField(2)]
    private DateTime _plantedTime;

    private const int BaseLogYield = 40;
    private const double FruitTreeYearlyGrowth = 1.10; // +10%/year
    private const double NonFruitTreeYearlyGrowth = 1.30; // oak/walnut — +30%/year

    [Constructible]
    public MahaonTree(MahaonTreeSpecies species = MahaonTreeSpecies.Apple)
        : base(MahaonTreeSpeciesTable.Get(species).TrunkGraphic)
    {
        Movable = false;
        _species = species;
        Name = MahaonTreeSpeciesTable.Get(species).NameRu;
    }

    /// <summary>Places this trunk in the world along with its paired foliage item, at the
    /// same spot. Use this instead of the plain MoveToWorld for a MahaonTree.</summary>
    public void PlantAt(Point3D loc, Map map)
    {
        MoveToWorld(loc, map);
        _plantedTime = Core.Now;
        _foliage = new MahaonTreeFoliage(_species);
        _foliage.MoveToWorld(loc, map);
    }

    public int YearsOld()
    {
        var yearLength = TimeSpan.FromTicks(SeasonSystem.SeasonLength.Ticks * 4);
        return (int)((Core.Now - _plantedTime).Ticks / yearLength.Ticks);
    }

    private const int MaxLogYield = 10_000;

    public int CurrentLogYield()
    {
        var growthRate = MahaonTreeSpeciesTable.Get(_species).BearsFruit
            ? FruitTreeYearlyGrowth
            : NonFruitTreeYearlyGrowth;

        var raw = BaseLogYield * Math.Pow(growthRate, YearsOld());
        return (int)Math.Min(MaxLogYield, Math.Round(raw));
    }

    // Difficulty scales with age but caps at 120 (our real skill cap) — so a fully-trained
    // lumberjack always has a real, high chance no matter how old the tree gets, instead of
    // needing an impossible skill value. Below that cap, chance scales with skill/difficulty.
    private const double DifficultyPerYear = 15.0;
    private const double BaseDifficulty = 20.0;
    private const double SkillCap = 120.0;
    private const double MinFellChance = 0.05;
    private const double MaxFellChance = 0.95;

    public double FellDifficulty() => Math.Min(SkillCap, BaseDifficulty + DifficultyPerYear * YearsOld());

    public double FellChance(double lumberjackingSkill) =>
        Math.Clamp(lumberjackingSkill / FellDifficulty(), MinFellChance, MaxFellChance);

    private static readonly string[] FailMessages =
    {
        "Твой удар не повредил это могучее древо.",
        "Топор отскакивает от ствола, почти не оставив следа.",
        "Дерево даже не вздрогнуло от удара.",
        "Слишком крепкая древесина — этот удар не свалит дерево."
    };

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendMessage("Слишком далеко.");
            return;
        }

        var skill = from.Skills[SkillName.Lumberjacking].Value;
        var chance = FellChance(skill);

        if (Utility.RandomDouble() > chance)
        {
            from.Animate(13, 5, 1, true, false, 0);
            from.PlaySound(0x13E);
            from.SendMessage(0x22, FailMessages.RandomElement());
            return;
        }

        var amount = CurrentLogYield();
        var lumberjackingSkill = from.Skills[SkillName.Lumberjacking].Value;
        var woodType = MahaonResourceTiers.PickWood(lumberjackingSkill);
        var log = (Item)Activator.CreateInstance(woodType, amount);
        var loc = Location;
        var map = Map;

        log.MoveToWorld(loc, map); // right at the tree's own spot, one stacked pile — not the chopper's pack

        from.SendMessage(0x59, $"Ты валишь дерево одним махом — {amount} брёвен лежат прямо тут. (возраст: {YearsOld()} год(лет))");

        Delete(); // whole tree — trunk, foliage, and any fruit left on it — is gone for good
    }

    public override void OnDelete()
    {
        _foliage?.Delete();
        base.OnDelete();
    }
}
