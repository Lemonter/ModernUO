using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0xA4EA, 0xA4EB)]
[SerializationGenerator(0, false)]
public partial class DecorativeDungeonChair : Item
{
    [Constructible]
    public DecorativeDungeonChair() : base(0xA4EA)
    {
        Weight = 1;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1159470; // decorative dungeon chair
}
