using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Spellbooks/ConjurersGrimoire.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ConjurersGrimoire : Spellbook
{
    public override int LabelNumber => 1094799; // Conjurer's Grimoire

    [Constructible]
    public ConjurersGrimoire()
        : base()
    {
        Hue = 1157;
        Slayer = SlayerName.Silver;
        Attributes.LowerManaCost = 10;
        Attributes.BonusInt = 8;
        Attributes.SpellDamage = 15;
        SkillBonuses.SetValues(0, SkillName.Magery, 15.0);
    }
}
