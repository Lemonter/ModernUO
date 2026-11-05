using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/EternalGuardianStaff.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class EternalGuardianStaff : GnarledStaff
{
    public override int LabelNumber => 1112443; // Eternal Guardian Staff

    [Constructible]
    public EternalGuardianStaff()
    {
        Hue = 95;
        SkillBonuses.SetValues(0, SkillName.Mysticism, 15.0);
        Attributes.SpellDamage = 10;
        Attributes.LowerManaCost = 5;
        Attributes.SpellChanneling = 1;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
