using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Items/Deeds.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class HeroOfTheUnlovedTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1157649; // Hero of the Unloved

    [Constructible]
    public HeroOfTheUnlovedTitleDeed()
    {
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Items/Deeds.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SaviorOfTheDementedTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1157651; // Savior of the Demented

    [Constructible]
    public SaviorOfTheDementedTitleDeed()
    {
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Items/Deeds.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SlayerOfThePumpkinKingTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1157650; // Slayer of the Pumpkin King

    [Constructible]
    public SlayerOfThePumpkinKingTitleDeed()
    {
    }
}
