using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Lavaliere : GoldNecklace
{
    public override int LabelNumber => 1114843; // Lavaliere

    [Constructible]
    public Lavaliere()
    {
        Hue = 1194;
        // Kinetic eater (20) dropped: BaseJewel has no SA absorption container.
        Attributes.DefendChance = 10;
        Resistances.Physical = 15;
        Attributes.LowerManaCost = 10;
        Attributes.LowerRegCost = 20;
    }
}
