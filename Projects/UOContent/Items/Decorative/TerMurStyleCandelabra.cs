using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TerMurStyleCandelabra : BaseLight
{
    [Constructible]
    public TerMurStyleCandelabra() : base(0x4039)
    {
        Duration = TimeSpan.Zero; // никогда не прогорает
        Burning = false;
        Light = LightType.Circle225;
        Weight = 10.0;
    }

    public override int LitItemID => 0x40BE;

    public override int UnlitItemID => 0x4039;
}
