using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Phoenix/PhoenixHelm.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PhoenixHelm : NorseHelm
{
    [Constructible]
    public PhoenixHelm()
    {
        Hue = 0x8E;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1041609; // norse helm of the phoenix
}
