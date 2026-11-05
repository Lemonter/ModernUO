using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. SilverSerpentVenom в этом репозитории отсутствует, поэтому сбор
///     яда серебряного змея опущен: флакон остаётся пустой декоративной склянкой.
/// </summary>
[SerializationGenerator(0, false)]
public partial class EmptyVenomVial : Item
{
    [Constructible]
    public EmptyVenomVial() : base(0x0E24)
    {
        Weight = 1.0;
        Name = "пустая склянка для яда";
        Hue = 0;
    }

    public override int LabelNumber => 1112215; // empty venom vial
}
