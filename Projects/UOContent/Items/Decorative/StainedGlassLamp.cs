using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StainedGlassLamp : BaseLight
{
    [Constructible]
    public StainedGlassLamp() : base(0x4C50)
    {
        Duration = Burnout ? TimeSpan.FromMinutes(60) : TimeSpan.Zero;
        Burning = false;
        Light = LightType.Circle225;
        Weight = 1.0;
    }

    public override int LitItemID => 0x4C51;

    public override int UnlitItemID => 0x4C50;
}
