using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/KhaldunPainting.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0xA1DE, 0xA1DF)]
public partial class ZombiePainting : Item
{
    public override int LabelNumber => 1023744; // painting

    [Constructible]
    public ZombiePainting() : base(0x4C62)
    {
        Weight = 10.0;
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/KhaldunPainting.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0xA1E0, 0xA1E1)]
public partial class SkeletonPortrait : Item
{
    public override int LabelNumber => 1023744; // painting

    [Constructible]
    public SkeletonPortrait() : base(0xA1E0)
    {
        Weight = 10.0;
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/KhaldunPainting.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0xA1E2, 0xA1E3)]
public partial class LichPainting : Item
{
    public override int LabelNumber => 1023744; // painting

    [Constructible]
    public LichPainting() : base(0xA1E2)
    {
        Weight = 10.0;
    }
}
