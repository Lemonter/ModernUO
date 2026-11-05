using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Spellbooks/ClaininsSpellbook.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ClaininsSpellbook : Spellbook
{
    [Constructible]
    public ClaininsSpellbook()
        : base()
    {
        Hue = 0x84D;
        Attributes.SpellChanneling = 1;
        Attributes.RegenMana = 3;
        Attributes.Luck = 80;
        Attributes.LowerRegCost = 15;
    }

    public override int LabelNumber => 1073262; // Clainin's Spellbook - Museum of Vesper Replica
}
