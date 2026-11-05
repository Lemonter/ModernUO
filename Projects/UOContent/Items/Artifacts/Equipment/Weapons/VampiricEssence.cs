using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/VampiricEssence.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class VampiricEssence : Cutlass
{
    public override int LabelNumber => 1113873; // Vampiric Essence

    [Constructible]
    public VampiricEssence()
    {
        Hue = 39;
        WeaponAttributes.HitLeechHits = 100;
        WeaponAttributes.HitHarm = 50;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = 50;
        AosElementDamages.Cold = 100;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
