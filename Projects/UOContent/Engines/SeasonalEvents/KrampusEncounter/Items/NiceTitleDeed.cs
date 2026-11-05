using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/NiceTitleDeed.cs).</summary>
[SerializationGenerator(0, false)]
public partial class NiceTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => "Послушный";

    [Constructible]
    public NiceTitleDeed()
    {
    }
}
