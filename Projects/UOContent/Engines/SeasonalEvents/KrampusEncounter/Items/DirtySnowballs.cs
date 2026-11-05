using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/DirtySnowballs.cs).</summary>
[SerializationGenerator(0, false)]
public partial class DirtySnowballs : SnowPile
{
    public override int LabelNumber => 1158833; // dirty snowballs

    [Constructible]
    public DirtySnowballs()
    {
        ItemID = 0xE74;
        Hue = 1301;
    }
}
