using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SculptureWoodCanes : BaseLight
{
    [Constructible]
    public SculptureWoodCanes() : base(0xA49E)
    {
    }

    public override int LabelNumber => 1029241; // sculpture

    public override int LitItemID => 0xA49F;

    public override int UnlitItemID => 0xA49E;

    public override int LitSound => 480;

    public override int UnlitSound => 482;

    public override double DefaultWeight => 1.0;
}
