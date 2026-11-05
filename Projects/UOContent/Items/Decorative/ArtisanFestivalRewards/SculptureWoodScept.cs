using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SculptureWoodScept : BaseLight
{
    [Constructible]
    public SculptureWoodScept() : base(0xA4A8)
    {
    }

    public override int LabelNumber => 1029241; // sculpture

    public override int LitItemID => 0xA4A9;

    public override int UnlitItemID => 0xA4A8;

    public override int LitSound => 480;

    public override int UnlitSound => 482;

    public override double DefaultWeight => 1.0;
}
