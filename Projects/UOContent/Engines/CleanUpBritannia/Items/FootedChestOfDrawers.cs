using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0x0A30, 0x0A38)]
[SerializationGenerator(0, false)]
public partial class FootedChestOfDrawers : LockableContainer
{
    [Constructible]
    public FootedChestOfDrawers() : base(0x0A30)
    {
        Weight = 25.0;
    }

    public override int DefaultGumpID => 0x48;

    public override int DefaultDropSound => 0x42;

    public override int LabelNumber => 1151221; // footed chest of drawers
}
