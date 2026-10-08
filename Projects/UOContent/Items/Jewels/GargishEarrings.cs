using ModernUO.Serialization;

namespace Server.Items;

// Graphic reused from GoldEarrings — no primary-source OSI graphic ID found this session.
[SerializationGenerator(0, false)]
public partial class GargishEarrings : BaseEarrings
{
    [Constructible]
    public GargishEarrings() : base(0x1087)
    {
    }

    public override double DefaultWeight => 0.1;

    public override int RequiredRaces => Race.AllowGargoylesOnly;
}
