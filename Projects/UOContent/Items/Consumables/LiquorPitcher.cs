using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BegLiquorPitcher : Pitcher
{
    [Constructible]
    public BegLiquorPitcher() : base(BeverageType.Liquor)
    {
        ItemID = Utility.RandomDouble() > 0.5 ? 8089 : 8090;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1075129); // Acquired by begging
    }
}
