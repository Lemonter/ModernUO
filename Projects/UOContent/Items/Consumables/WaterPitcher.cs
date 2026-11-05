using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BegWaterPitcher : Pitcher
{
    [Constructible]
    public BegWaterPitcher() : base(BeverageType.Water)
    {
        ItemID = Utility.RandomDouble() > 0.5 ? 4088 : 4089;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1075129); // Acquired by begging
    }
}
