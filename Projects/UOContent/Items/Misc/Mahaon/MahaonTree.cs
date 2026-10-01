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
    private const double FruitTreeYearlyGrowth = 1.01; // +1%/year — was +10%/year, growth speed cut by 10x
    private const double NonFruitTreeYearlyGrowth = 1.03; // oak/walnut — +3%/year — was +30%/year

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

    public long CurrentLogYield()
    {
        var growthRate = MahaonTreeSpeciesTable.Get(_species).BearsFruit
            ? FruitTreeYearlyGrowth
            : NonFruitTreeYearlyGrowth;

        // No more cap — an old enough tree can genuinely yield a huge pile of logs now.
        // The 10x-slower growth rate above is what actually keeps this in check day to
        // day; whoever hangs onto one tree for years earns the payoff. Logs get split
        // into multiple stacks at felling time (a single Item's Amount tops out at
        // 60000) instead of truncating or overflowing one stack.
        var raw = BaseLogYield * Math.Pow(growthRate, YearsOld());
        return (long)Math.Round(raw);
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

        var amount = Systems.MahaonWorld.MahaonHouseFenceSystem.ApplyYieldBonus(CurrentLogYield(), Location, Map);
        var lumberjackingSkill = from.Skills[SkillName.Lumberjacking].Value;
        var woodType = MahaonResourceTiers.PickWood(lumberjackingSkill);
        var loc = Location;
        var map = Map;

        // A single Item's Amount tops out at 60000 — split a huge yield into as many full
        // stacks as needed instead of silently truncating or overflowing one stack (same
        // approach as the fisherman's bag bulk-carve).
        const long maxStack = 60_000;
        var remaining = amount;
        var stacksCreated = 0;
        var caughtByBag = false;

        while (remaining > 0)
        {
            var thisStack = (int)Math.Min(remaining, maxStack);
            remaining -= thisStack;
            stacksCreated++;

            var log = (Item)Activator.CreateInstance(woodType, thisStack);

            if (Systems.MahaonWorld.MahaonResourceBagSystem.TryGive(from, log))
            {
                caughtByBag = true;
            }
            else
            {
                log.MoveToWorld(loc, map); // right at the tree's own spot — not the chopper's pack
            }
        }

        from.SendMessage(
            0x59,
            (caughtByBag
                ? $"Ты валишь дерево одним махом — {amount} брёвен сразу уходят в сумку лесоруба."
                : $"Ты валишь дерево одним махом — {amount} брёвен лежат прямо тут.")
            + (stacksCreated > 1 ? $" ({stacksCreated} стопок — больше 60000 в одну не помещается)" : "")
            + $" (возраст: {YearsOld()} год(лет))"
        );

        Delete(); // whole tree — trunk, foliage, and any fruit left on it — is gone for good
    }

    public override void OnDelete()
    {
        _foliage?.Delete();
        base.OnDelete();
    }
}
