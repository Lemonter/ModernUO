using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Each catch is its own fish with its own real weight (1-20 stones, same "random per
///     catch" idea as the engine's own BigFish, just a smaller range for an everyday fish
///     rather than a trophy one) — not stackable, since a uniform per-unit weight wouldn't
///     make sense once every fish is a different size. Carve() derives the steak count from
///     that same weight instead of a flat multiplier, so a big fish actually cuts into more
///     steaks than a small one.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Fish : Item, ICarvable
{
    [Constructible]
    public Fish() : base(Utility.Random(0x09CC, 4))
    {
        Weight = Utility.RandomMinMax(1, 20);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1070858, $"{(int)Weight}"); // ~1_weight~ stones — same cliloc BigFish uses
    }

    public void Carve(Mobile from, Item item)
    {
        ScissorHelper(from, new RawFishSteak(), System.Math.Max(1, (int)Weight / 4), false);
    }
}
