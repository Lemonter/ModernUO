using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Phoenix/PhoenixGloves.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PhoenixGloves : RingmailGloves
{
    [Constructible]
    public PhoenixGloves()
    {
        Hue = 0x8E;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1041605; // ringmail gloves of the phoenix
}
