using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA4E8, 0xA4E9)]
[SerializationGenerator(0, false)]
public partial class DecorativeWallHook : Item
{
    [Constructible]
    public DecorativeWallHook() : base(0xA4E8)
    {
        Weight = 1;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1159469; // decorative wall hook
}
