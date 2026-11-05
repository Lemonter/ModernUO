using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Phoenix/PhoenixChest.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PhoenixChest : RingmailChest
{
    [Constructible]
    public PhoenixChest()
    {
        Hue = 0x8E;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1041606; // ringmail tunic of the phoenix
}
