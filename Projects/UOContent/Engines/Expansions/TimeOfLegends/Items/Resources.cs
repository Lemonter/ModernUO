using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Time of Legends tribal resources, ported from ServUO
///     (Scripts/Services/Expansions/Time Of Legends/Items/Resources.cs).
///
///     The three tiger pelts and the dragon turtle scute are what the Eodon tribes pay for and what
///     <see cref="Mobiles.DragonTurtle" /> and the wild tigers shed when carved. All four are
///     <see cref="ICommodity" /> so commodity deeds accept them.
/// </summary>
[SerializationGenerator(0, false)]
[Flippable(0x9BCC, 0x9BCD)]
public partial class TigerPelt : Item, ICommodity
{
    [Constructible]
    public TigerPelt(int amount = 1) : base(0x9BCC)
    {
        Weight = 1.0;
        Stackable = true;
        Amount = amount;

        Hue = 243;
    }

    public override int LabelNumber => 1123908; // tiger pelt

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}

[SerializationGenerator(0, false)]
public partial class WhiteTigerPelt : Item, ICommodity
{
    [Constructible]
    public WhiteTigerPelt(int amount = 1) : base(0x9BCC)
    {
        Weight = 1.0;
        Stackable = true;
        Amount = amount;
        Hue = 0;
    }

    public override int LabelNumber => 1156201; // white tiger pelt

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}

[SerializationGenerator(0, false)]
public partial class BlackTigerPelt : Item, ICommodity
{
    [Constructible]
    public BlackTigerPelt(int amount = 1) : base(0x9BCC)
    {
        Weight = 1.0;
        Stackable = true;
        Amount = amount;
        Hue = 1175;
    }

    public override int LabelNumber => 1156200; // black tiger pelt

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}

[SerializationGenerator(0, false)]
public partial class DragonTurtleScute : Item, ICommodity
{
    [Constructible]
    public DragonTurtleScute(int amount = 1) : base(0x9BCE)
    {
        Weight = 1.0;
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1123910; // dragon turtle scute

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}
