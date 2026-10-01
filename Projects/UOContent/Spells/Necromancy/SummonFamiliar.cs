using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.Necromancy;

public class SummonFamiliarSpell : NecromancerSpell
{
    private static readonly SpellInfo _info = new(
        "Summon Familiar",
        "Kal Xen Bal",
        203,
        9031,
        Reagent.BatWing,
        Reagent.GraveDust,
        Reagent.DaemonBlood
    );

    public SummonFamiliarSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

    public override double RequiredSkill => 30.0;
    public override int RequiredMana => 17;

    // Mahaon: was one familiar per caster (Dictionary<Mobile, BaseCreature>). The
    // summoning mastery now grants extra slots, so this holds a list. Nothing outside
    // this file pokes the dictionary directly any more — use FindFamiliar/IsFamiliar/
    // CountFamiliars below.
    private static readonly Dictionary<Mobile, List<BaseCreature>> _table = new();

    /// <summary>Live familiars of this master, pruning any that have been deleted.</summary>
    public static int CountFamiliars(Mobile master)
    {
        if (master == null || !_table.TryGetValue(master, out var list))
        {
            return 0;
        }

        for (var i = list.Count - 1; i >= 0; --i)
        {
            if (list[i]?.Deleted != false)
            {
                list.RemoveAt(i);
            }
        }

        if (list.Count == 0)
        {
            _table.Remove(master);
            return 0;
        }

        return list.Count;
    }

    /// <summary>First live familiar of the given type, or null. Replaces the old
    /// "Table.TryGetValue(...) &amp;&amp; bc is T" pattern at every call site.</summary>
    public static T FindFamiliar<T>(Mobile master) where T : BaseCreature
    {
        if (master == null || !_table.TryGetValue(master, out var list))
        {
            return null;
        }

        for (var i = 0; i < list.Count; ++i)
        {
            if (list[i] is T match && !match.Deleted)
            {
                return match;
            }
        }

        return null;
    }

    public static bool IsFamiliar(Mobile master, Mobile creature)
    {
        if (master == null || creature == null || !_table.TryGetValue(master, out var list))
        {
            return false;
        }

        return creature is BaseCreature bc && list.Contains(bc);
    }

    public static void Add(Mobile master, BaseCreature familiar)
    {
        if (master == null || familiar == null)
        {
            return;
        }

        if (!_table.TryGetValue(master, out var list))
        {
            _table[master] = list = new List<BaseCreature>();
        }

        list.Add(familiar);
    }

    public static SummonFamiliarEntry[] Entries { get; } =
    {
        new(typeof(HordeMinionFamiliar), 1060146, 30.0, 30.0), // Horde Minion
        new(typeof(ShadowWispFamiliar), 1060142, 50.0, 50.0),  // Shadow Wisp
        new(typeof(DarkWolfFamiliar), 1060143, 60.0, 60.0),    // Dark Wolf
        new(typeof(DeathAdder), 1060145, 80.0, 80.0),          // Death Adder
        new(typeof(VampireBatFamiliar), 1060144, 100.0, 100.0) // Vampire Bat
    };

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void RemoveEffects(Mobile m)
    {
        if (m == null || !_table.Remove(m, out var list))
        {
            return;
        }

        foreach (var summon in list)
        {
            summon?.Delete();
        }

        list.Clear();
    }

    public static void Unregister(Mobile master, Mobile summoned)
    {
        if (master == null || summoned == null || !_table.TryGetValue(master, out var list))
        {
            return;
        }

        if (summoned is not BaseCreature bc)
        {
            return;
        }

        if (list.Remove(bc) && list.Count == 0)
        {
            _table.Remove(master);
        }
    }

    public override bool CheckCast()
    {
        var limit = Systems.MahaonCombat.NecromancySummonSystem.GetFamiliarLimit(Caster);

        if (CountFamiliars(Caster) >= limit)
        {
            // Mahaon: one familiar plus one per 50 Школа призыва, so the message has to
            // say how many you are actually allowed instead of the flat 1061605.
            Caster.SendMessage(0x3B2, $"Больше фамильяров тебе не удержать ({limit}).");
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Caster.SendGump(new SummonFamiliarGump(Caster, Entries, this));
        }

        FinishSequence();
    }
}

public class SummonFamiliarEntry
{
    public SummonFamiliarEntry(Type type, TextDefinition name, double reqNecromancy, double reqSpiritSpeak)
    {
        Type = type;
        Name = name;
        ReqNecromancy = reqNecromancy;
        ReqSpiritSpeak = reqSpiritSpeak;
    }

    public Type Type { get; }

    public TextDefinition Name { get; }

    public double ReqNecromancy { get; }

    public double ReqSpiritSpeak { get; }
}

public class SummonFamiliarGump : DynamicGump
{
    private const int EnabledColor16 = 0x0F20;
    private const int DisabledColor16 = 0x262A;

    private const int EnabledColor32 = 0x18CD00;
    private const int DisabledColor32 = 0x4A8B52;

    private readonly Mobile _from;
    private readonly SummonFamiliarEntry[] _entries;

    private readonly SummonFamiliarSpell _spell;

    public override bool Singleton => true;

    public SummonFamiliarGump(Mobile from, SummonFamiliarEntry[] entries, SummonFamiliarSpell spell) : base(200, 100)
    {
        _from = from;
        _entries = entries;
        _spell = spell;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(10, 10, 250, 178, 9270);
        builder.AddAlphaRegion(20, 20, 230, 158);

        builder.AddImage(220, 20, 10464);
        builder.AddImage(220, 72, 10464);
        builder.AddImage(220, 124, 10464);

        builder.AddItem(188, 16, 6883);
        builder.AddItem(198, 168, 6881);
        builder.AddItem(8, 15, 6882);
        builder.AddItem(2, 168, 6880);

        builder.AddHtmlLocalized(30, 26, 200, 20, 1060147, EnabledColor16); // Chose thy familiar...

        var necro = _from.Skills.Necromancy.Value;
        var spirit = _from.Skills.SpiritSpeak.Value;

        for (var i = 0; i < _entries.Length; ++i)
        {
            var entry = _entries[i];
            var name = entry.Name;

            var enabled = necro >= entry.ReqNecromancy && spirit >= entry.ReqSpiritSpeak;

            builder.AddButton(27, 53 + i * 21, 9702, 9703, i + 1);

            name.AddHtmlText(
                ref builder,
                50,
                51 + i * 21,
                150,
                20,
                numberColor: enabled ? EnabledColor16 : DisabledColor16,
                stringColor: enabled ? EnabledColor32 : DisabledColor32
            );
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var index = info.ButtonID - 1;

        if (index < 0 || index >= _entries.Length)
        {
            _from.SendLocalizedMessage(1061825); // You decide not to summon a familiar.
            return;
        }

        var entry = _entries[index];

        var necro = _from.Skills.Necromancy.Value;
        var spirit = _from.Skills.SpiritSpeak.Value;

        if ((_from as PlayerMobile)?.DuelContext?.AllowSpellCast(_from, _spell) == false)
        {
        }
        else if (SummonFamiliarSpell.CountFamiliars(_from) >=
                 Systems.MahaonCombat.NecromancySummonSystem.GetFamiliarLimit(_from))
        {
            _from.SendLocalizedMessage(1061605); // You already have a familiar.
        }
        else if (necro < entry.ReqNecromancy || spirit < entry.ReqSpiritSpeak)
        {
            // That familiar requires ~1_NECROMANCY~ Necromancy and ~2_SPIRIT~ Spirit Speak.
            _from.SendLocalizedMessage(1061606, $"{entry.ReqNecromancy:F1}\t{entry.ReqSpiritSpeak:F1}");

            _from.SendGump(this);
        }
        else if (entry.Type == null)
        {
            _from.SendMessage("Такой фамильяр ещё не описан.");
            _from.SendGump(this);
        }
        else
        {
            try
            {
                var bc = entry.Type.CreateInstance<BaseCreature>();

                // TODO: Is this right?
                bc.Skills.MagicResist.Base = _from.Skills.MagicResist.Base;

                // Mahaon: Школа призыва also makes the familiar itself tougher — it is a
                // fixed-stat creature in vanilla, so without this the mastery would only
                // ever add head-count.
                Systems.MahaonCombat.NecromancySummonSystem.ApplyMasteryPower(_from, bc);

                if (BaseCreature.Summon(bc, _from, _from.Location, -1, TimeSpan.FromDays(1.0)))
                {
                    _from.FixedParticles(0x3728, 1, 10, 9910, EffectLayer.Head);
                    bc.PlaySound(bc.GetIdleSound());
                    SummonFamiliarSpell.Add(_from, bc);

                    Systems.MahaonCombat.NecromancySummonSystem.AnnounceSummon(
                        _from,
                        bc,
                        SummonFamiliarSpell.CountFamiliars(_from),
                        Systems.MahaonCombat.NecromancySummonSystem.GetFamiliarLimit(_from)
                    );
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
