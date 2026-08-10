using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Mahaon custom currency: lowest denomination.
///     10 MahaonCopper = 1 Silver, 100 Silver = 1 Gold (see <see cref="CurrencyHelper" />).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonCopper : Item
{
    [Constructible]
    public MahaonCopper(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    [Constructible]
    public MahaonCopper(int amount = 1) : base(0xEED)
    {
        Stackable = true;
        Amount = amount;
        Hue = 0x96; // coppery hue to distinguish from gold at a glance
    }

    public override double DefaultWeight => Core.ML ? 0.02 / 3 : 0.02;

    public override string DefaultName => Amount == 1 ? "медная монета" : "медные монеты";
}
