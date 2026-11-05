using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Resource/EtherealSand.cs), where it sat outside the ForsakenFoes folder.
///     It is the commodity the Mining Cooperative Merchant sells, so it is ported next to that merchant rather than
///     dropped with the rest of the High Seas resource set. ServUO implemented the commodity description with
///     <c>TextDefinition</c>; here it is the plain <see cref="ICommodity.DescriptionNumber" /> cliloc.
/// </summary>
[SerializationGenerator(0, false)]
public partial class EtherealSand : Item, ICommodity
{
    public override int LabelNumber => 1125984; // ethereal sand

    public override double DefaultWeight => 0.1;

    [Constructible]
    public EtherealSand() : this(1)
    {
    }

    [Constructible]
    public EtherealSand(int amount) : base(0xA3E8)
    {
        Stackable = true;
        Amount = amount;
    }

    [Constructible]
    public EtherealSand(int amountFrom, int amountTo) : this(Utility.RandomMinMax(amountFrom, amountTo))
    {
    }

    int ICommodity.DescriptionNumber => LabelNumber;

    bool ICommodity.IsDeedable => true;
}
