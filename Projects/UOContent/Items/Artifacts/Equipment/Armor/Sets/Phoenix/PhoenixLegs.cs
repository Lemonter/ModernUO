using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Phoenix/PhoenixLegs.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PhoenixLegs : RingmailLegs
{
    [Constructible]
    public PhoenixLegs()
    {
        Hue = 0x8E;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1041608; // ringmail leggings of the phoenix
}
