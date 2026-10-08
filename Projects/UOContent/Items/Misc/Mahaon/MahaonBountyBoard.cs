using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Placeable via [AddLem → Квесты. Double-click shows the current bounty (see
///     Systems.MahaonQuests.BountyHunterSystem) — no gump needed for something this
///     simple, just an overhead-style message.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonBountyBoard : Item
{
    [Constructible]
    public MahaonBountyBoard() : base(0x1E5E) // bulletin-board-style graphic
    {
        Movable = false;
        Name = "доска охотников за головами";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        from.SendMessage(0x59, Systems.MahaonQuests.BountyHunterSystem.CurrentBountyText());
    }
}
