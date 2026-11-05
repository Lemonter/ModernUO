using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Phoenix/PhoenixArms.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class PhoenixArms : RingmailArms
{
    [Constructible]
    public PhoenixArms()
    {
        Hue = 0x8E;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1041607; // ringmail sleeves of the phoenix
}
