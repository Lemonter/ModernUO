using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecorativeDungeonStool : Item
{
    [Constructible]
    public DecorativeDungeonStool() : base(0xA4EC)
    {
        Weight = 1;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1159471; // decorative dungeon stool
}
