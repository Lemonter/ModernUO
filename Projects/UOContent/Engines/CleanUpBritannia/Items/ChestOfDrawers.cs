using ModernUO.Serialization;

namespace Server.Items;

[Furniture]
[Flippable(0x0A2C, 0x0A34)]
[SerializationGenerator(0, false)]
public partial class ChestOfDrawers : LockableContainer
{
    [Constructible]
    public ChestOfDrawers() : base(0x0A2C)
    {
        Weight = 25.0;
    }

    public override int DefaultGumpID => 0x51;

    public override int DefaultDropSound => 0x42;

    public override int LabelNumber => 1022604; // chest of drawers
}
