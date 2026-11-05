using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/PunisherTitleDeed.cs).</summary>
[SerializationGenerator(0, false)]
public partial class PunisherTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => "Каратель";

    [Constructible]
    public PunisherTitleDeed()
    {
    }
}
