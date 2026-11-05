using ModernUO.Serialization;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. GMItemPacket из LOSBlocker в репозитории отсутствует, поэтому
///     невидимая плитка использует обычный пакет мира.
/// </summary>
[SerializationGenerator(0, false)]
public partial class InvisibleTile : Item
{
    [Constructible]
    public InvisibleTile() : base(0x2198)
    {
        Movable = false;
    }

    public override string DefaultName => "невидимая плитка";
}

[SerializationGenerator(0, false)]
public partial class RevealTile : Item
{
    [Constructible]
    public RevealTile() : base(0x2e46)
    {
        Movable = false;
    }

    public override int GetUpdateRange(Mobile m) => m.Player ? 1 : base.GetUpdateRange(m);
}
