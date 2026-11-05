using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/KhaldunFirstAidBelt.cs).</summary>
[SerializationGenerator(0, false)]
public partial class KhaldunFirstAidBelt : FirstAidBelt
{
    [Constructible]
    public KhaldunFirstAidBelt()
    {
        LootType = LootType.Blessed;
        WeightReduction = 50;
        Attributes.RegenHits = 2;
        HealingBonus = 10;
    }
}
