using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Blackthorn Revamped dungeon (Items/BlackthornPainting.cs) — the two wall
///     paintings that hang in Blackthorn's castle. Flippable attribute renamed to this engine's spelling.
/// </summary>
[Flippable(0x4C62, 0x4C63)]
[SerializationGenerator(0, false)]
public partial class BlackthornPainting1 : Item
{
    public override int LabelNumber => 1023744; // painting

    [Constructible]
    public BlackthornPainting1() : base(0x4C62)
    {
    }
}

[Flippable(0x4C64, 0x4C65)]
[SerializationGenerator(0, false)]
public partial class BlackthornPainting2 : Item
{
    public override int LabelNumber => 1023744; // painting

    [Constructible]
    public BlackthornPainting2() : base(0x4C64)
    {
    }
}
