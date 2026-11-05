using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA4EF, 0xA4F0)]
[SerializationGenerator(0, false)]
public partial class DecorativeDungeonMask : Item
{
    [Constructible]
    public DecorativeDungeonMask() : base(0xA4EF)
    {
        Weight = 1;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1159473; // decorative dungeon mask
}
