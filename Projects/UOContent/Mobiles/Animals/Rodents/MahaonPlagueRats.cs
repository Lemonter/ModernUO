using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     "Infected" rat variants for MahaonVersaSystem's plague event — HitPoison/
///     HitPoisonChance on BaseCreature are read-only virtual properties (no setter), so
///     these can't just be set on a plain Rat/GiantRat instance at spawn time; they need
///     an actual override like this instead.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonPlagueRat : Rat
{
    [Constructible]
    public MahaonPlagueRat()
    {
        Hue = 0x453; // sickly green, visually distinct from a normal rat
    }

    public override Poison HitPoison => Poison.Lesser;
    public override double HitPoisonChance => 0.4;
}

[SerializationGenerator(0, false)]
public partial class MahaonPlagueGiantRat : GiantRat
{
    [Constructible]
    public MahaonPlagueGiantRat()
    {
        Hue = 0x453;
    }

    public override Poison HitPoison => Poison.Lesser;
    public override double HitPoisonChance => 0.4;
}
