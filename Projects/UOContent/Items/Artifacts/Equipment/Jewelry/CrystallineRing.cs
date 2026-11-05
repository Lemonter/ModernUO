using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Jewelry/CrystallineRing.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CrystallineRing : GoldRing
{
    public override int LabelNumber => 1075096; // Crystalline Ring

    [Constructible]
    public CrystallineRing()
    {
        Hue = 0x480;
        Attributes.RegenHits = 5;
        Attributes.RegenMana = 3;
        Attributes.SpellDamage = 20;
        SkillBonuses.SetValues(0, SkillName.Magery, 20.0);
        SkillBonuses.SetValues(1, SkillName.Focus, 20.0);
    }
}
