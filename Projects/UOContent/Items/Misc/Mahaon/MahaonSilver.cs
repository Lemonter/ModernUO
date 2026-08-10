using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Mahaon custom currency: mid denomination.
///     10 Copper = 1 MahaonSilver, 100 MahaonSilver = 1 Gold (see <see cref="CurrencyHelper" />).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonSilver : Item
{
    [Constructible]
    public MahaonSilver(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public MahaonSilver(int amount = 1) : base(0xEED)
    {
        Stackable = true;
        Amount = amount;
        Hue = 0x480; // silvery hue to distinguish from gold/copper at a glance
    }

    public override double DefaultWeight => Core.ML ? 0.02 / 3 : 0.02;

    public override string DefaultName => Amount == 1 ? "серебряная монета" : "серебряные монеты";
}
