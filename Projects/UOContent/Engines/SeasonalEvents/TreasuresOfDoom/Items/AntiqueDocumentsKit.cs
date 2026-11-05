using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/AntiqueDocumentsKit.cs).</summary>
[SerializationGenerator(0, false)]
public partial class AntiqueDocumentsKit : Item
{
    public override int LabelNumber => 1155630; // Antique Documents Kit

    [Constructible]
    public AntiqueDocumentsKit() : base(0x1EBB)
    {
        LootType = LootType.Blessed;
    }
}
