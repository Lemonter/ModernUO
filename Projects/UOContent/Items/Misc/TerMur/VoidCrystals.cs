using ModernUO.Serialization;

namespace Server.Items;

/// <summary>The three void crystals, ported from ServUO (Scripts/Items/Quest/Void Crystals.cs).
/// Which one a void manifestation carries is set when it is summoned, by the rouser that called
/// it — the rouser's own type decides.</summary>
[SerializationGenerator(0, false)]
public partial class VoidCrystalOfCorruptedArcaneEssence : Item
{
    [Constructible]
    public VoidCrystalOfCorruptedArcaneEssence() : base(0x1F19) => Hue = 1267;

    public override int LabelNumber => 1150321; // Void Crystal of Corrupted Arcane Essence
}

[SerializationGenerator(0, false)]
public partial class VoidCrystalOfCorruptedSpiritualEssence : Item
{
    [Constructible]
    public VoidCrystalOfCorruptedSpiritualEssence() : base(0x1F19) => Hue = 1269;

    public override int LabelNumber => 1150322; // Void Crystal of Corrupted Spiritual Essence
}

[SerializationGenerator(0, false)]
public partial class VoidCrystalOfCorruptedMysticalEssence : Item
{
    [Constructible]
    public VoidCrystalOfCorruptedMysticalEssence() : base(0x1F19) => Hue = 2717;

    public override int LabelNumber => 1150323; // Void Crystal of Corrupted Mystical Essence
}
