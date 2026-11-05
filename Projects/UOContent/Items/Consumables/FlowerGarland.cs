using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BegFlowerGarland : FlowerGarland
{
    [Constructible]
    public BegFlowerGarland()
    {
        ItemID = Utility.RandomDouble() > 0.5 ? 8965 : 8966;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1075129); // Acquired by begging
    }
}
