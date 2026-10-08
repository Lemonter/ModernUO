using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GargoyleHalfApron : BaseWaist
{
    [Constructible]
    public GargoyleHalfApron(int hue = 0) : base(0x153B, hue)
    {
    }

    public override double DefaultWeight => 2.0;

    public override int RequiredRaces => Race.AllowGargoylesOnly;
}
