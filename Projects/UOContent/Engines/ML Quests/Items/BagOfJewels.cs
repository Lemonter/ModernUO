using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BagOfJewels : Item
{
    public override int LabelNumber => 1075307; // Bag of Jewels

    [Constructible]
    public BagOfJewels() : base(0xE76)
    {
        LootType = LootType.Blessed;
        Weight = 2.0;
        Hue = 155;
    }

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        list.Add(1075453); // Contents: 30 jewels
    }
}
