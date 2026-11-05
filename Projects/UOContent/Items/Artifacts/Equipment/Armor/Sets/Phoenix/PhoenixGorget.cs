using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Phoenix/PhoenixGorget.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PhoenixGorget : StuddedGorget
{
    [Constructible]
    public PhoenixGorget()
    {
        Hue = 0x8E;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1041604; // studded gorget of the phoenix
}
