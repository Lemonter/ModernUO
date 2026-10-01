using ModernUO.Serialization;

namespace Server.Items;

public enum MahaonResourceCategory
{
    Ore,       // Сумка шахтёра
    Wood,      // Сумка лесоруба
    Fish,      // Сумка рыбака
    Reagent,   // Сумка алхимика (включая ингредиенты некромантии)
    Crop,      // Сумка фермера
    Hunter     // Сумка охотника — шкуры/шерсть/перья с туш + расходники (зелья/бинты/свитки/боеприпасы)
}

/// <summary>
///     Base for the five "magic" gathering bags — MahaonResourceBagSystem.TryGive routes
///     matching resources straight into one of these instead of the backpack, and whatever
///     ends up inside weighs nothing: UpdateTotal is overridden to always report a Weight
///     delta of 0 up the ownership chain, so the bag (and everything above it — backpack,
///     player) never feels the weight of its contents. Item COUNT still totals normally
///     (MaxItems still applies, just raised well above a normal bag), only weight is free.
/// </summary>
[SerializationGenerator(0, false)]
public abstract partial class MahaonResourceBag : Bag
{
    public abstract MahaonResourceCategory Category { get; }

    protected MahaonResourceBag()
    {
        Movable = true;
        LootType = LootType.Blessed; // инструмент профессии — не должен теряться при смерти
    }

    public override int MaxWeight => 0; // 0 = no weight cap at all (same convention as bank boxes)
    public override int DefaultMaxItems => 300; // still finite — just generous, not literally infinite

    public override void UpdateTotal(Item sender, TotalType type, int delta)
    {
        if (type == TotalType.Weight && sender != this)
        {
            delta = 0;
        }

        base.UpdateTotal(sender, type, delta);
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonMinerBag : MahaonResourceBag
{
    public override MahaonResourceCategory Category => MahaonResourceCategory.Ore;
    public override string DefaultName => "Сумка шахтёра";

    [Constructible]
    public MahaonMinerBag() => Hue = 0x455; // shadow-iron grey, reads as "mining"
}

[SerializationGenerator(0, false)]
public partial class MahaonLumberjackBag : MahaonResourceBag
{
    public override MahaonResourceCategory Category => MahaonResourceCategory.Wood;
    public override string DefaultName => "Сумка лесоруба";

    [Constructible]
    public MahaonLumberjackBag() => Hue = 0x3A2; // bark brown
}

[SerializationGenerator(0, false)]
public partial class MahaonFishermanBag : MahaonResourceBag, ICarvable
{
    public override MahaonResourceCategory Category => MahaonResourceCategory.Fish;
    public override string DefaultName => "Сумка рыбака";

    // Fish don't stack anymore (each catch has its own weight — see Fish.cs), so a long
    // fishing session needs real headroom here, well above the other bags' 300.
    public override int DefaultMaxItems => 1500;

    [Constructible]
    public MahaonFishermanBag() => Hue = 0x489; // sea blue

    /// <summary>Target this bag with any bladed item (same ICarvable hook a single Fish
    /// uses — Targets/BladedItemTarget.cs calls Carve on whatever gets targeted) to gut
    /// every fish inside at once instead of one at a time. Produces a single stacked pile
    /// of steaks — no need to fish out and carve a thousand individual catches by hand.</summary>
    public void Carve(Mobile from, Item item)
    {
        var toCarve = new System.Collections.Generic.List<Item>();

        foreach (var contained in Items)
        {
            if (contained is Fish or BigFish)
            {
                toCarve.Add(contained);
            }
        }

        if (toCarve.Count == 0)
        {
            from.SendMessage(0x22, "В сумке нет рыбы для разделки.");
            return;
        }

        var totalSteaks = 0;

        foreach (var fish in toCarve)
        {
            totalSteaks += System.Math.Max(1, (int)fish.Weight / 4);
            fish.Delete();
        }

        // A single Item's Amount tops out at 60000 (same ceiling ScissorHelper itself
        // respects) — split into as many full stacks as needed instead of silently
        // truncating or overflowing one stack.
        const int maxStack = 60_000;
        var stacksCreated = 0;
        var remaining = totalSteaks;

        while (remaining > 0)
        {
            var thisStack = System.Math.Min(remaining, maxStack);
            remaining -= thisStack;
            stacksCreated++;

            var steaks = new RawFishSteak(thisStack);

            if (!TryDropItem(from, steaks, false))
            {
                steaks.MoveToWorld(from.Location, from.Map);
            }
        }

        from.SendMessage(
            0x59,
            stacksCreated > 1
                ? $"Разделано рыбы: {toCarve.Count}, получено стейков: {totalSteaks} ({stacksCreated} стопок — больше 60000 в одну не помещается)."
                : $"Разделано рыбы: {toCarve.Count}, получено стейков: {totalSteaks}."
        );
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonAlchemistBag : MahaonResourceBag
{
    public override MahaonResourceCategory Category => MahaonResourceCategory.Reagent;
    public override string DefaultName => "Сумка алхимика";

    [Constructible]
    public MahaonAlchemistBag() => Hue = 0x54; // reagent-pouch green
}

[SerializationGenerator(0, false)]
public partial class MahaonFarmerBag : MahaonResourceBag
{
    public override MahaonResourceCategory Category => MahaonResourceCategory.Crop;
    public override string DefaultName => "Сумка фермера";

    [Constructible]
    public MahaonFarmerBag() => Hue = 0x8A5; // wheat gold
}

[SerializationGenerator(0, false)]
public partial class MahaonHunterBag : MahaonResourceBag
{
    public override MahaonResourceCategory Category => MahaonResourceCategory.Hunter;
    public override string DefaultName => "Сумка охотника";

    [Constructible]
    public MahaonHunterBag() => Hue = 0x21; // blood red
}
