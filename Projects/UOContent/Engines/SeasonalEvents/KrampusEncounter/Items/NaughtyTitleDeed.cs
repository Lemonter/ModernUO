using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/NaughtyTitleDeed.cs).</summary>
[SerializationGenerator(0, false)]
public partial class NaughtyTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => "Непослушный";

    [Constructible]
    public NaughtyTitleDeed()
    {
    }
}
