using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Spellbooks/FallenMysticsSpellbook.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FallenMysticsSpellbook : Spellbook
{
    public override int LabelNumber => 1113867; // Fallen Mystic's Spellbook

    [Constructible]
    public FallenMysticsSpellbook()
        : this((ulong)0)
    {
    }

    public FallenMysticsSpellbook(ulong content)
        : base(content, 0x2D9D)
    {
        Hue = 687;
        SkillBonuses.SetValues(0, SkillName.Mysticism, 10.0);
        Attributes.LowerManaCost = 5;
        Attributes.RegenMana = 1;
        Attributes.LowerRegCost = 10;
        Attributes.CastRecovery = 1;
        Attributes.CastSpeed = 1;
        Attributes.SpellDamage = 10;
        Slayer = SlayerName.Fey;
    }

    public override SpellbookType SpellbookType => SpellbookType.Mystic;

    public override int BookOffset
    {
        get
        {
            return 677;
        }
    }

    public override int BookCount => 16;
}
