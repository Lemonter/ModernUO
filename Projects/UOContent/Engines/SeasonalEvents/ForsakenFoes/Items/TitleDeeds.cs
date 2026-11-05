using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/CaptainTitleDeed.cs,
///     CommanderTitleDeed.cs and EnsignTitleDeed.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CaptainTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1159216; // Captain

    [Constructible]
    public CaptainTitleDeed()
    {
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/CommanderTitleDeed.cs).</summary>
[SerializationGenerator(0, false)]
public partial class CommanderTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1159215; // Commander

    [Constructible]
    public CommanderTitleDeed()
    {
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/EnsignTitleDeed.cs).</summary>
[SerializationGenerator(0, false)]
public partial class EnsignTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1159214; // Ensign

    [Constructible]
    public EnsignTitleDeed()
    {
    }
}
