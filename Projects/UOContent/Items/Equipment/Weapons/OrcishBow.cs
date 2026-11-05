using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Weapons/OrcishBow.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class OrcishBow : Bow
{
    public override int LabelNumber => 1153778; // an orcish bow

    [Constructible]
    public OrcishBow()
    {
        Hue = 1107;
        Attributes.WeaponDamage = 25;
        WeaponAttributes.DurabilityBonus = 70;
    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        list.Add(1060410, WeaponAttributes.DurabilityBonus.ToString()); // durability ~1_val~%
    }
}
