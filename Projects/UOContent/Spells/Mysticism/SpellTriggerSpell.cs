using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.Mysticism;

/// <summary>One entry in Spell Trigger's menu: which spell a stone can be charged with, what
/// it looks like, and how much combined skill it costs to pick. Rank is 1-6 and the
/// requirement is Rank * 40 against Mysticism + Focus/Imbuing, exactly as the original.</summary>
public record SpellTriggerDef(int SpellId, int Rank, int NameCliloc, int TooltipCliloc, int ItemId)
{
    public int RequiredSkill => Rank * 40;
}

/// <summary>Mysticism 685. Ported from ServUO
/// (Scripts/Spells/Mysticism/SpellDefinitions/SpellTriggerSpell.cs); registration was already
/// in Spells/Initializer.cs, commented out.
///
/// Charges a Spell Stone with one of eleven lower-circle Mysticism spells, to be released
/// later without paying its cast time. Picking is done through a gump, so the spell is cast
/// in two halves: a bare cast opens the menu and stops, and the gump starts a second cast
/// carrying the chosen definition.
///
/// Nether Cyclone, Hail Storm, Spell Plague and Rising Colossus are absent from the roster in
/// the original too — Spell Trigger deliberately can't bottle the top of the school.</summary>
public class SpellTriggerSpell : MysticSpell
{
    private static readonly SpellInfo _info = new(
        "Spell Trigger",
        "In Vas Ort Ex",
        230,
        9022,
        Reagent.Garlic,
        Reagent.MandrakeRoot,
        Reagent.SpidersSilk,
        Reagent.DragonsBlood
    );

    public static readonly SpellTriggerDef[] Definitions =
    {
        new(677, 1, 1031678, 1095193, 0x2D9E), // Nether Bolt
        new(678, 1, 1031679, 1095194, 0x2D9F), // Healing Stone
        new(679, 2, 1031680, 1095195, 0x2DA0), // Purge Magic
        new(680, 2, 1031681, 1095196, 0x2DA1), // Enchant
        new(681, 3, 1031682, 1095197, 0x2DA2), // Sleep
        new(682, 3, 1031683, 1095198, 0x2DA3), // Eagle Strike
        new(683, 4, 1031684, 1095199, 0x2DA4), // Animated Weapon
        new(684, 4, 1031685, 1095200, 0x2DA5), // Stone Form
        new(686, 5, 1031687, 1095202, 0x2DA7), // Mass Sleep
        new(687, 6, 1031688, 1095203, 0x2DA8), // Cleansing Winds
        new(688, 6, 1031689, 1095204, 0x2DA9)  // Bombard
    };

    private readonly SpellTriggerDef _definition;

    public SpellTriggerSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public SpellTriggerSpell(Mobile caster, Item scroll, SpellTriggerDef definition)
        : base(caster, scroll, _info)
    {
        _definition = definition;
    }

    public override SpellCircle Circle => SpellCircle.Fifth;

    public override bool CheckCast()
    {
        if (_definition == null)
        {
            // Nothing chosen yet — put the menu up and abandon this cast.
            SpellTriggerGump.DisplayTo(Caster, Scroll);
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            var pack = Caster.Backpack;

            if (pack != null)
            {
                // Only one stone at a time.
                foreach (var old in pack.FindItemsByType<SpellStone>())
                {
                    old.Delete();
                }

                var stone = new SpellStone(_definition);

                if (Caster.PlaceInBackpack(stone))
                {
                    Caster.PlaySound(0x659);
                    Caster.SendLocalizedMessage(1080165); // A Spell Stone appears in your backpack.
                }
                else
                {
                    stone.Delete();
                    Caster.SendLocalizedMessage(502385); // Your pack cannot hold this item.
                }
            }
        }

        FinishSequence();
    }
}

/// <summary>The charged stone. Releases its spell on double click and is consumed; a
/// five-minute cooldown stops a mystic keeping one permanently primed.</summary>
[SerializationGenerator(0, false)]
public partial class SpellStone : SpellScroll
{
    private static readonly Dictionary<Mobile, DateTime> _cooldown = new();

    [SerializableField(0)]
    private int _nameCliloc;

    [SerializableField(1)]
    private int _tooltipCliloc;

    [Constructible]
    public SpellStone(int spellId = 677, int nameCliloc = 1031678, int tooltipCliloc = 1095193, int itemId = 0x2D9E)
        : base(spellId, 0x4079)
    {
        Stackable = false;
        Amount = 1;
        LootType = LootType.Blessed;

        _nameCliloc = nameCliloc;
        _tooltipCliloc = tooltipCliloc;
        Hue = 0;

        // The definition's graphic is the tooltip icon rather than the ground graphic; the
        // stone itself is always 0x4079, as in the original.
        _ = itemId;
    }

    public SpellStone(SpellTriggerDef def)
        : this(def.SpellId, def.NameCliloc, def.TooltipCliloc, def.ItemId)
    {
    }

    public override int LabelNumber => _nameCliloc;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1080166, $"{_tooltipCliloc:#}"); // Use: ~1_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_cooldown.TryGetValue(from, out var until) && until > Core.Now)
        {
            var seconds = (int)(until - Core.Now).TotalSeconds;

            // You must wait ~1_seconds~ seconds before you can use this item.
            from.SendLocalizedMessage(1079263, seconds.ToString());
            return;
        }

        _cooldown[from] = Core.Now + TimeSpan.FromSeconds(300);

        base.OnDoubleClick(from);

        if (!Deleted)
        {
            Delete();
        }
    }
}

/// <summary>Spell Trigger's picker. Two columns, ten rows a page, paginated because the
/// roster is eleven entries long.</summary>
public class SpellTriggerGump : Gump
{
    private const int PerPage = 10;

    private readonly int _page;
    private readonly Item _scroll;

    private SpellTriggerGump(Mobile from, Item scroll, int page) : base(60, 36)
    {
        _scroll = scroll;
        _page = page;

        var skill = from.Skills.Mysticism.Value + Math.Max(from.Skills.Imbuing.Value, from.Skills.Focus.Value);

        AddPage(0);
        AddBackground(0, 0, 520, 404, 0x13BE);

        AddHtmlLocalized(0, 12, 520, 16, 1080151, "", 0, false, false); // Choose a spell to trigger

        var start = page * PerPage;
        var end = Math.Min(start + PerPage, SpellTriggerSpell.Definitions.Length);

        for (var i = start; i < end; i++)
        {
            var def = SpellTriggerSpell.Definitions[i];
            var slot = i - start;

            var x = slot < 5 ? 30 : 275;
            var y = 50 + slot % 5 * 65;

            AddImageTiled(x, y, 200, 60, slot < 5 ? 0x918 : 0x919);
            AddItem(x + 6, y + 6, def.ItemId);

            // Below the skill requirement the row is shown but not clickable.
            if (skill >= def.RequiredSkill)
            {
                AddButton(x + 60, y + 20, 0x837, 0x838, i + 100);
            }

            AddHtmlLocalized(x + 85, y + 18, 110, 32, def.NameCliloc, "", 0, false, false);
        }

        var pages = (SpellTriggerSpell.Definitions.Length + PerPage - 1) / PerPage;

        if (page > 0)
        {
            AddButton(30, 370, 0x15E3, 0x15E7, 1); // back
        }

        if (page < pages - 1)
        {
            AddButton(470, 370, 0x15E1, 0x15E5, 2); // next
        }

        AddButton(250, 370, 0x47E, 0x480, 0); // cancel
    }

    /// <summary>Rule #13 — the roster is a non-empty static array, but the caster still has to
    /// be a player with a client to send it to.</summary>
    public static void DisplayTo(Mobile from, Item scroll)
    {
        if (from is not PlayerMobile pm || pm.NetState == null)
        {
            return;
        }

        pm.CloseGump<SpellTriggerGump>();
        pm.SendGump(new SpellTriggerGump(pm, scroll, 0));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        switch (info.ButtonID)
        {
            case 0:
                {
                    return;
                }
            case 1:
                {
                    from.SendGump(new SpellTriggerGump(from, _scroll, _page - 1));
                    return;
                }
            case 2:
                {
                    from.SendGump(new SpellTriggerGump(from, _scroll, _page + 1));
                    return;
                }
        }

        var index = info.ButtonID - 100;

        if (index < 0 || index >= SpellTriggerSpell.Definitions.Length)
        {
            return;
        }

        new SpellTriggerSpell(from, _scroll, SpellTriggerSpell.Definitions[index]).Cast();
    }
}
