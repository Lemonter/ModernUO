using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/ProphetTitleDeed.cs,
///     SeekerOfTheFallenStarTitleDeed.cs, ZealotOfKhalAnkurTitleDeed.cs and Quest/Items/GumshoeTitleDeed.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ProphetTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1158683; // Prophet

    [Constructible]
    public ProphetTitleDeed()
    {
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/SeekerOfTheFallenStarTitleDeed.cs).</summary>
[SerializationGenerator(0, false)]
public partial class SeekerOfTheFallenStarTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1158682; // Seeker of the Fallen Star

    [Constructible]
    public SeekerOfTheFallenStarTitleDeed()
    {
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/ZealotOfKhalAnkurTitleDeed.cs).</summary>
[SerializationGenerator(0, false)]
public partial class ZealotOfKhalAnkurTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1158684; // Zealot of Khal Ankur

    [Constructible]
    public ZealotOfKhalAnkurTitleDeed()
    {
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Quest/Items/GumshoeTitleDeed.cs).</summary>
[SerializationGenerator(0, false)]
public partial class GumshoeTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1158638; // Gumshoe

    [Constructible]
    public GumshoeTitleDeed()
    {
    }
}
