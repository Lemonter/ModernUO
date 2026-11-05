using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x403F, 0x4040)]
[SerializationGenerator(0, false)]
public partial class GargishSculpture : Item
{
    [Constructible]
    public GargishSculpture() : base(0x403F)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override double DefaultWeight => 1.0;
}
