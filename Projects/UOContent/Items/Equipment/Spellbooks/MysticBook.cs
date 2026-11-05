using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Spellbooks/MysticBook.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MysticBook : Spellbook
{
    [Constructible]
    public MysticBook()
        : this((ulong)0)
    {
    }

    public MysticBook(ulong content)
        : base(content, 0x2D9D)
    {
        this.Layer = Layer.OneHanded;
    }

    public override SpellbookType SpellbookType => SpellbookType.Mystic;
    public override int BookOffset => 677;
    public override int BookCount => 16;
}
