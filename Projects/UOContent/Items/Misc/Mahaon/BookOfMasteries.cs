using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Spells.SkillMasteries;
using Server.Systems.MahaonMasteries;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Items/Equipment/Spellbooks/
// BookOfMasteries.cs). Real ServUO subclasses their full Spellbook framework (SpellbookType/
// BookOffset/BookCount, a bitmask of which spells are "written in" the book) — not needed
// here, since this book's only real job is opening the mastery-selection gump via its
// context menu; a plain Item does that just as well without dragging in spellbook
// mechanics this doesn't use.
[SerializationGenerator(0, false)]
public partial class BookOfMasteries : Item
{
    private static readonly Dictionary<Mobile, DateTime> Cooldown = new();

    [Constructible]
    public BookOfMasteries() : base(0x225A)
    {
        Weight = 3.0;
        Name = "книга мастерства";
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        var entry = new SwitchMasteryEntry(this);

        if (!IsChildOf(from.Backpack) || !CheckCooldown(from))
        {
            entry.Enabled = false;
        }

        list.Add(entry);
    }

    private class SwitchMasteryEntry : ContextMenuEntry
    {
        private readonly BookOfMasteries _book;

        public SwitchMasteryEntry(BookOfMasteries book) : base(1151948) => _book = book; // Switch Mastery

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from is PlayerMobile pm && _book.IsChildOf(from.Backpack) && CheckCooldown(from))
            {
                from.CloseGump<MasterySelectionGump>();
                from.SendGump(new MasterySelectionGump(pm));
            }
        }
    }

    public static void AddToCooldown(Mobile from) => Cooldown[from] = Core.Now + TimeSpan.FromMinutes(10);

    public static bool CheckCooldown(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        if (!Cooldown.TryGetValue(from, out var until))
        {
            return true;
        }

        if (until < Core.Now)
        {
            Cooldown.Remove(from);
            return true;
        }

        return false;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (RootParent is Mobile m)
        {
            var sk = MasteryState.GetCurrentMastery(m);

            if (sk != SkillName.Alchemy)
            {
                list.Add(MasteryInfo.GetLocalization(sk));
            }
        }
    }
}
