using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/RangersCloakOfAugmentation.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class RangersCloakOfAugmentation : Cloak
{
    public override int LabelNumber => 1115514; // Ranger's Cloak Of Augmentation

    [Constructible]
    public RangersCloakOfAugmentation()
    {
        Hue = 0x54A;
        Attributes.SpellDamage = 3;
        Attributes.LowerManaCost = 1;
        Attributes.WeaponSpeed = 5;
    }
}
