using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Отброшено (отсутствует в репозитории): IsArtifact, Siege.SiegeShard,
///     SimpleContextMenuEntry и счётчик Corpse.PlayerCorpses (проверка «слишком много призывов»).
/// </summary>
[SerializationGenerator(0, false)]
public partial class UndertakersStaff : GnarledStaff
{
    private static readonly Dictionary<Mobile, SummonState> _summons = new();

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private int _charges;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _summonAll;

    [Constructible]
    public UndertakersStaff()
    {
        LootType = LootType.Blessed;

        _charges = 100;
        _summonAll = true;
    }

    public override int LabelNumber => 1071498; // Undertaker's Staff

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1071518, _summonAll ? "#1071508" : "#1071507");
        list.Add(1060584, _charges); // uses remaining: ~1_val~
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> entries)
    {
        base.GetContextMenuEntries(from, ref entries);

        if (!IsChildOf(from.Backpack))
        {
            return;
        }

        var recent = new StaffMenuEntry(1071507, () =>
        {
            _summonAll = false;
            InvalidateProperties();
        });

        var all = new StaffMenuEntry(1071508, () =>
        {
            _summonAll = true;
            InvalidateProperties();
        });

        if (_summonAll)
        {
            all.Flags |= CMEFlags.Highlighted;
        }
        else
        {
            recent.Flags |= CMEFlags.Highlighted;
        }

        recent.Enabled = !IsSummoning();
        all.Enabled = !IsSummoning();

        entries.Add(recent);
        entries.Add(all);
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!_summons.ContainsKey(m) && (IsChildOf(m.Backpack) || m.FindItemOnLayer(Layer) == this))
        {
            TryGetCorpse(m);
        }
    }

    private void TryGetCorpse(Mobile m)
    {
        if (!CanGetCorpse(m))
        {
            return;
        }

        m.PlaySound(0xF5);

        if (_summonAll)
        {
            var corpses = GetCorpses(m);

            if (corpses == null)
            {
                // The staff glows slightly, then fades...
                m.SendLocalizedMessage(1071511);
                return;
            }

            // The staff reaches out to ~1_COUNT~ of your corpses and tries to draw them to you...
            m.SendLocalizedMessage(1071527, $"{corpses.Count}");
            BeginSummon(m, corpses);
        }
        else
        {
            var corpse = GetCorpse(m);

            if (corpse == null)
            {
                m.SendLocalizedMessage(1071511);
                return;
            }

            // The staff reaches out to your corpse and tries to draw it to you...
            m.SendLocalizedMessage(1071528);
            BeginSummon(m, [corpse]);
        }
    }

    private void BeginSummon(Mobile m, List<Corpse> corpses)
    {
        Timer.StartTimer(TimeSpan.FromSeconds(3), () => TryEndSummon(m, corpses), out var token);
        _summons[m] = new SummonState(this, token);
    }

    private static bool CanGetCorpse(Mobile m)
    {
        if (m.Criminal)
        {
            m.SendLocalizedMessage(1071510); // You are a criminal and cannot use this item.
            return false;
        }

        if (Spells.SpellHelper.CheckCombat(m))
        {
            m.SendLocalizedMessage(1071514); // You cannot use this item during the heat of battle.
            return false;
        }

        return true;
    }

    public void TryEndSummon(Mobile m, List<Corpse> corpses)
    {
        _summons.Remove(m);

        if (corpses == null || corpses.Count == 0)
        {
            m.SendLocalizedMessage(1071511);
            return;
        }

        var tooFar = false;
        var notEnoughTime = false;
        var success = true;

        if (_summonAll)
        {
            var copy = new List<Corpse>(corpses);

            foreach (var c in copy)
            {
                var remove = false;

                if (c.Map != m.Map)
                {
                    remove = true;
                    tooFar = true;
                }

                if (c.Killer is PlayerMobile && c.Killer != m &&
                    c.TimeOfDeath + TimeSpan.FromSeconds(180) > Core.Now)
                {
                    remove = true;
                    notEnoughTime = true;
                }

                if (remove)
                {
                    corpses.Remove(c);
                }
            }

            if (corpses.Count == 0)
            {
                success = false;
            }
        }
        else
        {
            var c = corpses[0];

            if (c.Map != m.Map)
            {
                tooFar = true;
            }

            if (c.Killer is PlayerMobile && c.Killer != m && c.TimeOfDeath + TimeSpan.FromSeconds(180) > Core.Now)
            {
                notEnoughTime = true;
            }

            if (tooFar || notEnoughTime)
            {
                if (tooFar)
                {
                    m.SendLocalizedMessage(1071512); // ...but the corpse is too far away!
                }
                else
                {
                    m.SendLocalizedMessage(1071515); // ...but not enough time has passed...
                }

                success = false;
            }
        }

        if (!success)
        {
            return;
        }

        m.PlaySound(0xFA);

        foreach (var c in corpses)
        {
            c.MoveToWorld(m.Location, m.Map);
        }

        if (_summonAll)
        {
            m.SendLocalizedMessage(1071530, $"{corpses.Count}"); // ...and succeeds in summoning ~1_COUNT~ of them!

            if (tooFar)
            {
                m.SendLocalizedMessage(1071513); // ...but one of them is too far away!
            }
            else if (notEnoughTime)
            {
                m.SendLocalizedMessage(1071516); // ...but one of them deflects the magic...
            }
        }
        else
        {
            m.SendLocalizedMessage(1071529); // ...and succeeds in the summoning of it!
        }

        if (_charges <= 0)
        {
            m.SendLocalizedMessage(1071509); // The staff has been reduced to pieces!
            Delete();
        }
        else
        {
            _charges--;
            InvalidateProperties();
        }
    }

    private static List<Corpse> GetCorpses(Mobile m) => m.Corpse is Corpse own ? [own] : null;

    private static Corpse GetCorpse(Mobile m) => m.Corpse as Corpse;

    public static bool TryRemoveTimer(Mobile m)
    {
        if (!_summons.Remove(m, out var state))
        {
            return false;
        }

        state.Token.Cancel();

        m.FixedEffect(0x3735, 6, 30);
        m.PlaySound(0x5C);

        m.SendLocalizedMessage(1071525); // You have been disrupted while attempting to pull your corpse!
        return true;
    }

    public bool IsSummoning()
    {
        foreach (var state in _summons.Values)
        {
            if (state.Staff == this)
            {
                return true;
            }
        }

        return false;
    }

    public override void OnAfterDelete()
    {
        foreach (var (mobile, state) in _summons)
        {
            if (state.Staff == this)
            {
                state.Token.Cancel();
                _summons.Remove(mobile);
                break;
            }
        }

        base.OnAfterDelete();
    }

    private readonly record struct SummonState(UndertakersStaff Staff, TimerExecutionToken Token);

    private class StaffMenuEntry : ContextMenuEntry
    {
        private readonly Action _action;

        public StaffMenuEntry(int number, Action action) : base(number) => _action = action;

        public override void OnClick(Mobile from, IEntity target) => _action();
    }
}
