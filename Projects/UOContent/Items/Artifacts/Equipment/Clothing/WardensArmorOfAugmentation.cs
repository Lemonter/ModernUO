using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/WardensArmorOfAugmentation.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class WardensArmorOfAugmentation : GargishLeatherWingArmor
{
    public override int LabelNumber => 1115515; // Warden's Armor Of Augmentation

    [Constructible]
    public WardensArmorOfAugmentation()
    {
        Hue = 0x9C2;
        Attributes.SpellDamage = 3;
        Attributes.LowerManaCost = 1;
        Attributes.WeaponSpeed = 5;
    }
}
