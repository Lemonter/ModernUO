using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/Maze of
// Death/UnderworldPuzzleBox.cs) — hands out a fresh UnderworldPuzzleItem once per 24h.
// `MetalChest` doesn't exist in this codebase — replaced with the generic
// `LockableContainer` base every other custom chest here already uses, just carrying the
// same ItemID.
[SerializationGenerator(0, false)]
public partial class UnderworldPuzzleBox : LockableContainer
{
    private static readonly Dictionary<Mobile, DateTime> Cooldowns = new();

    [Constructible]
    public UnderworldPuzzleBox() : base(3712)
    {
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(Location, 3))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (from.Player && IsInCooldown(from))
        {
            from.SendLocalizedMessage(1113386); // You are too tired to attempt solving more puzzles at this time.
            return;
        }

        if (from.Backpack == null)
        {
            return;
        }

        if (from.Backpack.FindItemByType(typeof(UnderworldPuzzleItem)) != null)
        {
            from.SendLocalizedMessage(501885); // You already own one of those!
            return;
        }

        var puzzle = new UnderworldPuzzleItem();
        from.AddToBackpack(puzzle);
        from.SendLocalizedMessage(1072223); // An item has been placed in your backpack.

        if (from.AccessLevel == AccessLevel.Player)
        {
            Cooldowns[from] = Core.Now + TimeSpan.FromHours(24);
        }
    }

    public static bool IsInCooldown(Mobile from)
    {
        if (Cooldowns.TryGetValue(from, out var expires))
        {
            if (expires < Core.Now)
            {
                Cooldowns.Remove(from);
                return false;
            }

            return true;
        }

        return false;
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        from.SendLocalizedMessage(1113513); // You cannot put items there.
        return false;
    }

    public override void DisplayTo(Mobile to) => to.SendLocalizedMessage(1005213); // You can't do that
}
