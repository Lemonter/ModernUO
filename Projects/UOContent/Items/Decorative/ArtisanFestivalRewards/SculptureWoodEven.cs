using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SculptureWoodEven : BaseLight
{
    [Constructible]
    public SculptureWoodEven() : base(0xA499)
    {
    }

    public override int LabelNumber => 1029241; // sculpture

    public override int LitItemID => 0xA49A;

    public override int UnlitItemID => 0xA499;

    public override int LitSound => 480;

    public override int UnlitSound => 482;

    public override double DefaultWeight => 1.0;
}
