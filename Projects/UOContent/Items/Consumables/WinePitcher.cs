using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BegWinePitcher : Pitcher
{
    [Constructible]
    public BegWinePitcher() : base(BeverageType.Wine)
    {
        ItemID = Utility.RandomDouble() > 0.5 ? 8091 : 8092;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1075129); // Acquired by begging
    }
}
