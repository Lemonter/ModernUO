using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/Branch.cs).</summary>
[SerializationGenerator(0, false)]
public partial class GoldBranch : Item
{
    public override int LabelNumber => 1158835; // branch

    [Constructible]
    public GoldBranch() : base(Utility.RandomList(3458, 3473))
    {
        Hue = 2721;
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/Branch.cs).</summary>
[SerializationGenerator(0, false)]
public partial class SilverBranch : Item
{
    public override int LabelNumber => 1158835; // branch

    [Constructible]
    public SilverBranch() : base(Utility.RandomList(3458, 3473))
    {
        Hue = 2500;
    }
}
