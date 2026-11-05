using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Talismans/FrostguardTalisman.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FrostguardTalisman : BaseTalisman
{
    public override int LabelNumber => 1115516; // Frostguard Talisman

    public override int ColdResistance => 3;

    [Constructible]
    public FrostguardTalisman()
        : base(0x2F5B)
    {
        Hue = 0x556;
        Weight = 1.0;
        Attributes.RegenMana = 1;
        Attributes.LowerManaCost = 5;
    }
}
