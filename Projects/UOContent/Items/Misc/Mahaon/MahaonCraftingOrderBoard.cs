using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Placeable via [AddLem → Квесты. Double-click either shows the current order (if
///     you don't have enough) or fulfills it on the spot (if you do) — see
///     Systems.MahaonQuests.CraftingGuildSystem.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonCraftingOrderBoard : Item
{
    [Constructible]
    public MahaonCraftingOrderBoard() : base(0x1E5E)
    {
        Movable = false;
        Name = "доска заказов гильдии ремесленников";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        Systems.MahaonQuests.CraftingGuildSystem.TryFulfill(from);
    }
}
