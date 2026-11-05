using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class OrigamiDragon : Item
{
    [Constructible]
    public OrigamiDragon() : base(0x4B1C)
    {
        Name = "изящный оригами-дракон";
        Weight = 1.0;
        LootType = LootType.Blessed;
    }
}

[SerializationGenerator(0, false)]
public partial class OrigamiBunny : Item
{
    [Constructible]
    public OrigamiBunny() : base(0x4B1F)
    {
        Name = "изящный оригами-кролик";
        Weight = 1.0;
        LootType = LootType.Blessed;
    }
}
