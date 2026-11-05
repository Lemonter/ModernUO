using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HornAbyssalInferno : Item
{
    [Constructible]
    public HornAbyssalInferno() : base(0x2DB7)
    {
    }

    public override int LabelNumber => 1031703; // Horn of Abyssal Infernal
}
