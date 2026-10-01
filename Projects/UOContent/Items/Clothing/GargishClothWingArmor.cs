using ModernUO.Serialization;

namespace Server.Items;

/// <summary>The cloth counterpart to the gargoyle leather wing armour already here. Ported from
/// ServUO (Scripts/Items/Equipment/Clothing/Cloaks.cs); Queen Zhah wears one.</summary>
[SerializationGenerator(0, false)]
public partial class GargishClothWingArmor : BaseClothing
{
    [Constructible]
    public GargishClothWingArmor(int hue = 0) : base(0x45A4, Layer.Cloak, hue)
    {
    }

    public override double DefaultWeight => 2.0;

    public override int AosStrReq => 10;
    public override int RequiredRaces => Race.AllowGargoylesOnly;
}
