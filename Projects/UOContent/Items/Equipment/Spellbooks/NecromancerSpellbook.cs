using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Spellbooks/NecromancerSpellbook.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CompleteNecromancerSpellbook : NecromancerSpellbook
{
    [Constructible]
    public CompleteNecromancerSpellbook()
        : base((ulong)0x1FFFF)
    {
    }
}
